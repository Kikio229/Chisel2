using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public class Hasher
{
    private ulong _state;

    public Hasher()
    {
        _state = 0x9E3779B185EBCA87;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint value)
    {
        Add((ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ulong value)
    {
        Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(float value)
    {
        Add(BitConverter.DoubleToUInt64Bits((double)value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double value)
    {
        Add(BitConverter.DoubleToUInt64Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Add(ReadOnlySpan<byte> data)
    {
        int offset = 0;

        // This is AI generated as fuck, all I did was slap an SSE optimization on top of it
        while (data.Length - offset >= 16)
        {
            ulong low, high;

            if (Sse2.IsSupported)
            {
                ulong[] values = new ulong[2];
                Vector128<byte> vec = Vector128.Create(data.Slice(offset, 16));
                Sse2.Store((byte*)&values, vec);

                low = values[0];
                high = values[1];
            }
            else
            {
                low = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset, 8));
                high = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset + 8, 8));
            }

            _state ^= low;
            _state *= 0x9E3779B185EBCA87;
            _state = Rotate(_state, 27);

            _state ^= high;
            _state *= 0xC2B2AE3D27D4EB4F;
            _state = Rotate(_state, 31);

            offset += 16;
        }

        while (data.Length - offset >= 8)
        {
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset, 8));
            Add(value);
            offset += 8;
        }

        while (offset < data.Length)
        {
            _state ^= data[offset];
            _state *= 0x100000001B3;
            ++offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Finalize32()
    {
        ulong x = Finalize64();
        return (uint)(x ^ (x >> 32)); // Folding 64 bits into 32
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Finalize64()
    {
        ulong x = _state;
        x ^= x >> 30;
        x *= 0xBF58476D1CE4E5B9;
        x ^= x >> 27;
        x *= 0x94D049BB133111EB;
        x ^= x >> 31;
        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rotate(ulong value, int offset)
    {
        return (value << offset) | (value >> (64 - offset));
    }
}