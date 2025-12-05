using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Sunfire.Logging;

namespace Sunfire.FSUtils;

public unsafe class MediaTypeScanner<TResult>
{
    private const int MaxSignatures = 512;
    private const int MaxOffset = 256;
    private const int TableSize = MaxOffset * 256;

    //Primary LUT [Offset << 8 | ByteValue] -> Mask of valid signatures.
    private readonly Vector512<byte>[] _lookupTable;

    //Used to detect if a signature is fully matched.
    private readonly Vector512<byte>[] _endMasks;

    //Maps signature ID to consumer result object.
    private readonly TResult[] _results;

    private int _nextBitId = 0;

    public MediaTypeScanner()
    {
        if (!Vector512.IsHardwareAccelerated)
        {
            _ = Logger.Warn(nameof(FSUtils), "AVX-512/Vector512 hardware acceleration is not active.");
        }

        _lookupTable = new Vector512<byte>[TableSize];
        _endMasks = new Vector512<byte>[MaxOffset];
        _results = new TResult[MaxSignatures];
    }

    public void AddSignature(ReadOnlySpan<byte> bytes, int startOffset, TResult result)
    {
        //Return on invalid add.
        if(_nextBitId >= MaxSignatures)
        {
            _ = Logger.Error(nameof(FSUtils), $"Attempted to add more than {MaxSignatures} signatures.");
            return;
        }
        else if(startOffset + bytes.Length > MaxOffset)
        {
            _ = Logger.Error(nameof(FSUtils), $"Attempted to add a signature which exceeds {MaxOffset} offset.");
            return;
        }
        else if(bytes.Length == 0)
        {
            _ = Logger.Error(nameof(FSUtils), "Attempted to add a signature with 0 bytes.");
            return;
        }

        //Get signature Id
        int sigId = _nextBitId++;
        _results[sigId] = result;

        Span<byte> buffer = stackalloc byte[64];
        int bytePos = sigId / 8;
        int bitPos = sigId % 8;
        buffer[bytePos] = (byte)(1 << bitPos);
        Vector512<byte> sigMask = Vector512.Create<byte>(buffer);

        //Populate wildcards into LUT if applicable
        for(int i = 0; i < startOffset; i++)
        {
            //set wildcard for this offset
            for (int val = 0; val < 256; val++)
            {
                int key = (i << 8) | val;

                _lookupTable[key] = Vector512.BitwiseOr(_lookupTable[key], sigMask);  
            }
        }

        //Populate magic bytes into LUT
        for(int i = 0; i < bytes.Length; i++)
        {
            byte val = bytes[i];

            int absOffset = i + startOffset;
            int key = (absOffset << 8) | val;

            _lookupTable[key] = Vector512.BitwiseOr(_lookupTable[key], sigMask);
        }

        //Mark end of sequence
        int endOffset = startOffset + bytes.Length - 1;
        _endMasks[endOffset] = Vector512.BitwiseOr(_endMasks[endOffset], sigMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public TResult? Lookup(ReadOnlySpan<byte> data)
    {
        //Start assuming all candidates are possible
        Vector512<byte> candidates = Vector512<byte>.AllBitsSet;

        int limit = Math.Min(data.Length, MaxOffset);

        for(int i = 0; i < limit; i ++)
        {
            int key = (i << 8) | data[i];

            //Filter candidates
            candidates = Vector512.BitwiseAnd(candidates, _lookupTable[key]);

            //Check if any remaining candidates just finished.
            Vector512<byte> winners = Vector512.BitwiseAnd(candidates, _endMasks[i]);

            //Return first match found
            if(winners != Vector512<byte>.Zero)
            {
                ulong* ptr = stackalloc ulong[8];
                winners.Store((byte*)ptr);
                for(int j = 0; j < 8; j++)
                {
                    if (ptr[j] != 0) 
                        return _results[(j * 64) + BitOperations.TrailingZeroCount(ptr[j])];
                }
                return default;
            }

            //Stop if no candidates left
            if (candidates == Vector512<byte>.Zero)
                return default;
        }

        return default;
    }
}
