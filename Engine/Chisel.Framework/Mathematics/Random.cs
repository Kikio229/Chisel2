using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public static class Random
{
    private static ulong _seed = 0;

    /* Bytes (8-bit) */

    public static byte GetByte()
    {
        return GetValue<byte>();
    }

    public static byte GetByteRange(byte min, byte max)
    {
        return (byte)(min + GetValue<byte>() % (max - min + 1));
    }

    public static byte[] GetByteArray(uint size)
    {
        byte[] result = new byte[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetByte();
        }

        return result;
    }

    public static byte[] GetByteArrayRange(byte min, byte max, uint size)
    {
        byte[] result = new byte[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetByteRange(min, max);
        }

        return result;
    }

    public static sbyte GetSbyte()
    {
        return GetValue<sbyte>();
    }

    public static sbyte GetSbyteRange(sbyte min, sbyte max)
    {
        return (sbyte)(min + GetValue<sbyte>() % (max - min + 1));
    }

    public static sbyte[] GetSbyteArray(uint size)
    {
        sbyte[] result = new sbyte[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetSbyte();
        }

        return result;
    }

    public static sbyte[] GetSbyteArrayRange(sbyte min, sbyte max, uint size)
    {
        sbyte[] result = new sbyte[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetSbyteRange(min, max);
        }

        return result;
    }

    /* Shorts (16-bit) */

    public static short GetShort()
    {
        return GetValue<short>();
    }

    public static short GetShortRange(short min, short max)
    {
        return (short)(min + GetValue<short>() % (max - min + 1));
    }

    public static short[] GetShortArray(uint size)
    {
        short[] result = new short[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetShort();
        }

        return result;
    }

    public static short[] GetShortArrayRange(short min, short max, uint size)
    {
        short[] result = new short[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetShortRange(min, max);
        }

        return result;
    }

    public static ushort GetUshort()
    {
        return GetValue<ushort>();
    }

    public static ushort GetUshortRange(ushort min, ushort max)
    {
        return (ushort)(min + GetValue<ushort>() % (max - min + 1));
    }

    public static ushort[] GetUshortArray(uint size)
    {
        ushort[] result = new ushort[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUshort();
        }

        return result;
    }

    public static ushort[] GetUshortArrayRange(ushort min, ushort max, uint size)
    {
        ushort[] result = new ushort[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUshortRange(min, max);
        }

        return result;
    }

    /* Ints (32-bit) */

    public static int GetInt()
    {
        return GetValue<int>();
    }

    public static int GetIntRange(int min, int max)
    {
        return (int)(min + GetValue<int>() % (max - min + 1));
    }

    public static int[] GetIntArray(uint size)
    {
        int[] result = new int[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetInt();
        }

        return result;
    }

    public static int[] GetIntArrayRange(int min, int max, uint size)
    {
        int[] result = new int[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetIntRange(min, max);
        }

        return result;
    }

    public static uint GetUint()
    {
        return GetValue<uint>();
    }

    public static uint GetUintRange(uint min, uint max)
    {
        return (uint)(min + GetValue<uint>() % (max - min + 1));
    }

    public static uint[] GetUintArray(uint size)
    {
        uint[] result = new uint[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUint();
        }

        return result;
    }

    public static uint[] GetUintArrayRange(uint min, uint max, uint size)
    {
        uint[] result = new uint[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUintRange(min, max);
        }

        return result;
    }

    /* Longs (64-bit) */

    public static long GetLong()
    {
        return GetValue<long>();
    }

    public static long GetLongRange(long min, long max)
    {
        return (long)(min + GetValue<long>() % (max - min + 1));
    }

    public static long[] GetLongArray(uint size)
    {
        long[] result = new long[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetLong();
        }

        return result;
    }

    public static long[] GetLongArrayRange(long min, long max, uint size)
    {
        long[] result = new long[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetLongRange(min, max);
        }

        return result;
    }

    public static ulong GetUlong()
    {
        return GetValue<ulong>();
    }

    public static ulong GetUlongRange(ulong min, ulong max)
    {
        return (ulong)(min + GetValue<ulong>() % (max - min + 1));
    }

    public static ulong[] GetUlongArray(uint size)
    {
        ulong[] result = new ulong[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUlong();
        }

        return result;
    }

    public static ulong[] GetUlongArrayRange(ulong min, ulong max, uint size)
    {
        ulong[] result = new ulong[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetUlongRange(min, max);
        }

        return result;
    }

    /* Floats (32-bit) */

    public static float GetFloat()
    {
        return GetValue<float>();
    }

    public static float GetFloatRange(float min, float max)
    {
        return (float)(min + GetValue<float>() % (max - min + 1));
    }

    public static float[] GetFloatArray(uint size)
    {
        float[] result = new float[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetFloat();
        }

        return result;
    }

    public static float[] GetFloatArrayRange(float min, float max, uint size)
    {
        float[] result = new float[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetFloatRange(min, max);
        }

        return result;
    }

    /* Doubles (64-bit) */

    public static double GetDouble()
    {
        return GetValue<double>();
    }

    public static double GetDoubleRange(double min, double max)
    {
        return (double)(min + GetValue<double>() % (max - min + 1));
    }

    public static double[] GetDoubleArray(uint size)
    {
        double[] result = new double[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetDouble();
        }

        return result;
    }

    public static double[] GetDoubleArrayRange(double min, double max, uint size)
    {
        double[] result = new double[size];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = GetDoubleRange(min, max);
        }

        return result;
    }

    // This is a variation of the method used in C
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T GetValue<T>()
    {
        _seed = (_seed + 1) * 1103515245 + 12345;
        _seed = _seed % (ulong)DateTime.Now.Ticks;

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.SByte => (T)(object)(sbyte)(_seed / 65536),
            TypeCode.Byte => (T)(object)(byte)(_seed / 65536),
            TypeCode.Int16 => (T)(object)(short)(_seed / 65536),
            TypeCode.UInt16 => (T)(object)(ushort)(_seed / 65536),
            TypeCode.Int32 => (T)(object)(int)(_seed / 65536),
            TypeCode.UInt32 => (T)(object)(uint)(_seed / 65536),
            TypeCode.Int64 => (T)(object)(long)(_seed / 65536),
            TypeCode.UInt64 => (T)(object)(ulong)(_seed / 65536),
            TypeCode.Single => (T)(object)((float)(_seed % float.MaxValue) / 65536),
            TypeCode.Double => (T)(object)((double)(_seed % double.MaxValue) / 65536),
            _ => throw new ArgumentException($"Random type of '{typeof(T)}' is not supported!")
        };
    }
}
