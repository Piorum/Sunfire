using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Sunfire.FSUtils.Models;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public unsafe class MediaTypeScanner<TResult>
{
    private const int MaxFastSignatures = 512;
    private const int MaxFastOffset = 256;
    private const int LutSize = MaxFastOffset * 256;

    private readonly Vector512<byte>[] primaryLut = new Vector512<byte>[LutSize];
    private readonly Vector512<byte>[] wildcardLut = new Vector512<byte>[MaxFastOffset];
    private readonly Vector512<byte>[] endMasks = new Vector512<byte>[MaxFastOffset];

    private readonly int[] fastSignatureLengths = new int[MaxFastSignatures];

    private readonly (TResult defaultResult, Dictionary<string, TResult>? extensionHints)[] fastResultMaps = new(TResult defaultResult, Dictionary<string, TResult>? extensionHints)[MaxFastSignatures];

    private int nextFastBitId = 0;
    private int largestFastOffset = 0;

    public MediaTypeScanner()
    {
        if (!Vector512.IsHardwareAccelerated)
            _ = Logger.Warn(nameof(FSUtils), "AVX-512 is not supported media scanning will be very slow.");
    }

    public void AddSignature(ReadOnlySpan<byte?> pattern, int offset, (TResult defaultResult, Dictionary<string, TResult>? extensionHints) resultMap)
    {
        if(pattern.Length == 0)
        {
            _ = Logger.Error(nameof(FSUtils), "Cannot add signature with empty pattern");
            return;
        }

        if(offset + pattern.Length <= MaxFastOffset)
            AddFastSignature(pattern, offset, resultMap);
        else
            AddSlowSignature(pattern, offset, resultMap);
    }

    private void AddFastSignature(ReadOnlySpan<byte?> pattern, int offset, (TResult defaultResult, Dictionary<string, TResult>? extensionHints) resultMap)
    {
        if(nextFastBitId >= MaxFastSignatures)
        {
            _ = Logger.Error(nameof(FSUtils), $"Fast signature limit {MaxFastSignatures} exceeded.");
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

    private void AddSlowSignature(ReadOnlySpan<byte?> pattern, int offset, (TResult defaultResult, Dictionary<string, TResult>? extensionHints) resultMap)
    {
        throw new NotImplementedException();
    }

    public TResult? Scan(FSEntry entry)
    {
        Span<byte> buffer = stackalloc byte[MaxFastOffset];

        if(!TryReadHeader(entry.Path, buffer, out int bytesRead))
        {
            return default;
        }
        
        ReadOnlySpan<byte> validData = buffer[..bytesRead];

        int bestMatch = ScanFast(validData);

        if(bestMatch != -1)
        {
            ref var map = ref fastResultMaps[bestMatch];

            return map.extensionHints is not null && map.extensionHints.TryGetValue(entry.Extension, out var result)
                ? result
                : map.defaultResult;
        }
        else
            return ScanSlow(entry);
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

    public TResult? ScanSlow(FSEntry entry)
    {
        //Implement slow scan

        return default;
    }

    public static bool TryReadHeader(string path, Span<byte> buffer, out int bytesRead)
    {
        bytesRead = 0;
        try
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);

            bytesRead = fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return true;
        }
        catch (FileNotFoundException) { /* File vanished between listing and reading */ }
        catch (DirectoryNotFoundException) { /* Parent moved */ }
        catch (UnauthorizedAccessException) { /* Permission denied */ }
        catch (IOException) { /* File strictly locked by another process (rare with FileShare.ReadWrite) */ }
        catch (System.Security.SecurityException) { /* ACL issues */ }

        return false;
    }
}
