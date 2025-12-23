using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public unsafe class FileTypeScanner<TResult>
{
    private const int MaxFastSignatures = 512;
    private int MaxFastOffset { get; init; }
    private int LutSize { get; init; }

    private readonly Vector512<byte>[] primaryLut;
    private readonly Vector512<byte>[] wildcardLut;
    private readonly Vector512<byte>[] endMasks;

    private readonly int[] fastSignatureLengths = new int[MaxFastSignatures];

    private readonly (TResult defaultResult, Dictionary<string, TResult>? extensionHints)[] fastResultMaps = new(TResult defaultResult, Dictionary<string, TResult>? extensionHints)[MaxFastSignatures];

    private int nextFastBitId = 0;
    private int largestFastOffset = 0;

    private readonly Dictionary<string, SlowSignature> slowSignatures = [];

    private struct SlowSignature()
    {
        required public byte?[] Pattern { get; init; }
        required public List<int> Offsets { get; init; }
        required public TResult ReturnValue;

        public bool FromEnd { get; init; }
    }

    private readonly Dictionary<string, TResult> scanResultsCache = [];

    public FileTypeScanner(int maxFastOffset = 512)
    {
        if (!Vector512.IsHardwareAccelerated)
            _ = Logger.Warn(nameof(FSUtils), "AVX-512 is not supported media scanning will be very slow");

        MaxFastOffset = maxFastOffset;
        LutSize = MaxFastOffset * 256;

        primaryLut = new Vector512<byte>[LutSize];
        wildcardLut = new Vector512<byte>[MaxFastOffset]; 
        endMasks = new Vector512<byte>[MaxFastOffset];
    }

    public void AddFastSignature(ReadOnlySpan<byte?> pattern, int offset, (TResult defaultResult, Dictionary<string, TResult>? extensionHints) resultMap)
    {
        if(pattern.Length == 0)
        {
            _ = Logger.Error(nameof(FSUtils), "Cannot add signature with empty pattern");
            return;
        }
        
        if(offset + pattern.Length >= MaxFastOffset)
        {
            _ = Logger.Error(nameof(FSUtils), $"Signature bounds extend past max offset of {MaxFastOffset}");
            return;
        }

        if(nextFastBitId >= MaxFastSignatures)
        {
            _ = Logger.Error(nameof(FSUtils), $"Fast signature limit {MaxFastSignatures} exceeded");
            return;
        }

        int sigId = nextFastBitId++;
        fastResultMaps[sigId] = resultMap;
        fastSignatureLengths[sigId] = pattern.Length;

        //Signature Mask
        Span<byte> buffer = stackalloc byte[64];
        buffer[sigId / 8] = (byte)(1 << (sigId % 8));
        Vector512<byte> sigMask = Vector512.Create<byte>(buffer);

        for(int i = 0; i < offset; i++)
        {
            wildcardLut[i] = Vector512.BitwiseOr(wildcardLut[i], sigMask);
        }

        for(int i = 0; i < pattern.Length; i++)
        {
            int absOffset = offset + i;
            byte? val = pattern[i];

            if(val is null)
            {
                wildcardLut[absOffset] = Vector512.BitwiseOr(wildcardLut[absOffset], sigMask);
            }
            else
            {
                var key = (absOffset << 8) | val.Value;

                primaryLut[key] = Vector512.BitwiseOr(primaryLut[key], sigMask);
            }
            
        }

        int totalOffset = offset + pattern.Length;
        int finalOffset = totalOffset - 1;
        endMasks[finalOffset] = Vector512.BitwiseOr(endMasks[finalOffset], sigMask);

        if(totalOffset > largestFastOffset)
            largestFastOffset = totalOffset;
    }

    public void AddSlowSignature(ReadOnlySpan<byte?> pattern, List<int> offsets, string extension, TResult returnValue, bool fromEnd = false)
    {
        if(pattern.Length == 0)
        {
            _ = Logger.Error(nameof(FSUtils), "Cannot add signature with empty pattern");
            return;
        }

        var sig = new SlowSignature()
        {
            Pattern = [.. pattern],
            Offsets = offsets,
            ReturnValue = returnValue,

            FromEnd = fromEnd
        };

        slowSignatures.Add(extension, sig);
    }

    public TResult? Scan(FSEntry entry)
    {
        var path = entry.Path;
        if(scanResultsCache.TryGetValue(path, out var returnValue))
            return returnValue;

        Stopwatch sw = new();
        sw.Start();

        int blockSize = 4096;
        Span<byte> buffer = stackalloc byte[blockSize];

        if(!FSHelpers.TryReadHeader(entry.Path, buffer, out int bytesRead))
        {
            return default;
        }
        
        ReadOnlySpan<byte> validData = buffer[..bytesRead];

        int bestMatch = ScanFast(validData);

        if(bestMatch != -1)
        {
            ref var map = ref fastResultMaps[bestMatch];

            returnValue = map.extensionHints is not null && map.extensionHints.TryGetValue(entry.Extension, out var result)
                ? result
                : map.defaultResult;
        }
        else
            returnValue = ScanSlow(entry, validData);

        if(returnValue is not null)
            scanResultsCache.Add(entry.Path, returnValue);

        sw.Stop();
        _ = Logger.Debug(nameof(FSUtils), $"Media type scan time {sw.Elapsed.TotalMicroseconds}us");

        return returnValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int ScanFast(ReadOnlySpan<byte> data)
    {
        Vector512<byte> candidates = Vector512<byte>.AllBitsSet;

        int limit = Math.Min(data.Length, largestFastOffset);

        int bestMatch = -1;
        int bestMatchLength = -1;

        ulong* ptr = stackalloc ulong[8];

        fixed (Vector512<byte>* tableBase = primaryLut)
        fixed (Vector512<byte>* wildcardBase = wildcardLut)
        fixed (Vector512<byte>* endMaskBase = endMasks)
        {
            for(int i = 0; i < limit; i++)
            {
                int key = (i << 8) | data[i];
                Vector512<byte> valid = Vector512.BitwiseOr(tableBase[key], wildcardBase[i]);
                candidates = Vector512.BitwiseAnd(candidates, valid);

                if(candidates == Vector512<byte>.Zero) 
                    return bestMatch;

                Vector512<byte> winners = Vector512.BitwiseAnd(candidates, endMaskBase[i]);
                if (winners != Vector512<byte>.Zero)
                {
                    winners.Store((byte*)ptr);
                    for (int j = 0; j < 8; j++)
                    {
                        ulong segment = ptr[j];
                        while(segment != 0)
                        {
                            int trail = BitOperations.TrailingZeroCount(segment);
                            int id = (j * 64) + trail;

                            if(fastSignatureLengths[id] > bestMatchLength)
                            {
                                bestMatchLength = fastSignatureLengths[id];
                                bestMatch = id;
                            }
                            segment &= ~(1UL << trail);
                        }
                    }
                }
            }
        }

        return bestMatch;
    }

    public TResult? ScanSlow(FSEntry entry, ReadOnlySpan<byte> firstBlock)
    {
        if(slowSignatures.Count == 0 || !slowSignatures.TryGetValue(entry.Extension, out var sig))
            return default;

        var pattern = sig.Pattern;

        bool valid = true;
        foreach(var offset in sig.Offsets)
        {
            ReadOnlySpan<byte> segment;

            if(!sig.FromEnd && sig.Pattern.Length + offset <= firstBlock.Length)
            {
                segment = firstBlock.Slice(offset, pattern.Length);
            }
            else if(sig.FromEnd)
            {
                var totalLength = pattern.Length + offset;
                byte[] buffer = new byte[totalLength];

                if(!FSHelpers.TryReadTail(entry.Path, buffer, out var bytesRead))
                    return default;

                if(bytesRead < totalLength)
                    return default;

                segment = buffer.AsSpan()[..pattern.Length];
            }
            else
            {
                byte[] buffer = new byte[pattern.Length];

                if(!FSHelpers.TryReadSegment(entry.Path, offset, pattern.Length, buffer))
                    return default;

                segment = buffer;
            }

            for(int i = 0; i < segment.Length; i++)
            {
                if(sig.Pattern[i] is not null && segment[i] != sig.Pattern[i])
                {
                    valid = false;
                    break;
                }
            }

            if(!valid)
                break;
        }

        if(valid)
            return sig.ReturnValue;

        return default;
    }
}
