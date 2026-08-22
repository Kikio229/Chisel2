using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Matrix3 : IEquatable<Matrix3>, IFormattable
{
    // Row 1

    public float M11
    {
        get => _top.GetElement(0);
        set => SetElement(_top, 0, value);
    }

    public float M12
    {
        get => _top.GetElement(1);
        set => SetElement(_top, 1, value);
    }

    public float M13
    {
        get => _top.GetElement(2);
        set => SetElement(_top, 2, value);
    }

    // Row 2

    public float M21
    {
        get => _middle.GetElement(0);
        set => SetElement(_middle, 0, value);
    }

    public float M22
    {
        get => _middle.GetElement(1);
        set => SetElement(_middle, 1, value);
    }

    public float M23
    {
        get => _middle.GetElement(2);
        set => SetElement(_middle, 2, value);
    }

    // Row 3

    public float M31
    {
        get => _bottom.GetElement(0);
        set => SetElement(_bottom, 0, value);
    }

    public float M32
    {
        get => _bottom.GetElement(1);
        set => SetElement(_bottom, 1, value);
    }

    public float M33
    {
        get => _bottom.GetElement(2);
        set => SetElement(_bottom, 2, value);
    }

    public static Matrix3 Zero => new Matrix3(0f);
    public static Matrix3 One => new Matrix3(1f);
    public static Matrix3 Identity = new Matrix3(
        1f, 0f, 0f,
        0f, 1f, 0f,
        0f, 0f, 1f);

    private Vector128<float> _top, _middle, _bottom;

    public Matrix3()
    {
        _top = Vector128.Create(0f, 0f, 0f, 0f);
        _middle = Vector128.Create(0f, 0f, 0f, 0f);
        _bottom = Vector128.Create(0f, 0f, 0f, 0f);
    }

    public Matrix3(float val)
    {
        _top = Vector128.Create(val, val, val, 0f);
        _middle = Vector128.Create(val, val, val, 0f);
        _bottom = Vector128.Create(val, val, val, 0f);
    }

    public Matrix3(Vector3 vec1, Vector3 vec2, Vector3 vec3)
    {
        _top = Vector128.Create(vec1.X, vec1.Y, vec1.Z, 0f);
        _middle = Vector128.Create(vec2.X, vec2.X, vec2.X, 0f);
        _bottom = Vector128.Create(vec3.X, vec3.X, vec3.X, 0f);
    }

    public Matrix3(float m11, float m12, float m13, float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        _top = Vector128.Create(m11, m12, m13, 0);
        _middle = Vector128.Create(m21, m22, m23, 0);
        _bottom = Vector128.Create(m31, m32, m33, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Matrix3(Vector128<float> top, Vector128<float> middle, Vector128<float> bottom)
    {
        _top = top;
        _middle = middle;
        _bottom = bottom;
    }

    public Matrix3 Negate()
    {
        Matrix3 result = Matrix3.Zero;

        if (MathUtilities.X86SimdSupported)
        {
            Vector128<float> mask = Vector128.Create(-0f);
            result = new Matrix3(Sse.Xor(_top, mask), Sse.Xor(_middle, mask), Sse.Xor(_bottom, mask));
            return result;
        }

        result.M11 = -M11;
        result.M12 = -M12;
        result.M13 = -M13;

        result.M21 = -M21;
        result.M22 = -M22;
        result.M23 = -M23;

        result.M31 = -M31;
        result.M32 = -M32;
        result.M33 = -M33;

        return result;
    }

    public System.Numerics.Matrix4x4 ToNumerics()
    {
        return new System.Numerics.Matrix4x4(
            M11, M12, M13, 0f,
            M21, M22, M23, 0f,
            M31, M32, M33, 0f,
            0f, 0f, 0f, 0f);
    }

    public string ToString(string format)
    {
        return ToString(format, null);
    }

    public string ToString(IFormatProvider formatProvider)
    {
        return ToString(null, formatProvider);
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(
            "({0}, {1}, {2},\n {3}, {4}, {5},\n {6}, {7}, {8})",
            M11.ToString(format, formatProvider),
            M12.ToString(format, formatProvider),
            M13.ToString(format, formatProvider),
            M21.ToString(format, formatProvider),
            M22.ToString(format, formatProvider),
            M23.ToString(format, formatProvider),
            M31.ToString(format, formatProvider),
            M32.ToString(format, formatProvider),
            M33.ToString(format, formatProvider));
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        Hasher hasher = new Hasher();
        hasher.Add(M11);
        hasher.Add(M12);
        hasher.Add(M13);
        hasher.Add(M21);
        hasher.Add(M22);
        hasher.Add(M23);
        hasher.Add(M31);
        hasher.Add(M32);
        hasher.Add(M33);
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Matrix3 other)
    {
        if (MathUtilities.X86SimdSupported)
        {
            Vector128<float> top, middle, bottom;
            top = Sse.CompareEqual(_top, other._top);
            middle = Sse.CompareEqual(_middle, other._middle);
            bottom = Sse.CompareEqual(_bottom, other._bottom);
            return (Sse.MoveMask(top) == 0xFF) && (Sse.MoveMask(middle) == 0xFF) && (Sse.MoveMask(bottom) == 0xFF);
        }

        return (M11 == other.M11) && (M12 == other.M12) && (M13 == other.M13) &&
            (M21 == other.M21) && (M22 == other.M22) && (M23 == other.M23) && 
            (M31 == other.M31) && (M32 == other.M32) && (M33 == other.M33);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Matrix3)
        {
            return Equals((Matrix3)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3 Add(Matrix3 left, Matrix3 right)
    {
        if (MathUtilities.X86SimdSupported)
        {
            Vector128<float> top, middle, bottom;
            top = Sse.Add(left._top, right._top);
            middle = Sse.Add(left._middle, right._middle);
            bottom = Sse.Add(left._bottom, right._bottom);
            return new Matrix3(top, middle, bottom);
        }

        return new Matrix3(left.M11 + right.M11,
            left.M12 + right.M12,
            left.M13 + right.M13,
            left.M21 + right.M21,
            left.M22 + right.M22,
            left.M23 + right.M23,
            left.M31 + right.M31,
            left.M32 + right.M32,
            left.M33 + right.M33);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix3 Multiply(Matrix3 left, Matrix3 right)
    {
        Matrix3 result = Matrix3.Zero;

        if (MathUtilities.X86SimdSupported)
        {
            Vector128<float> rrow1, rrow2, rrow3, lrow1, lrow2, lrow3;

            rrow1 = right._top;
            rrow2 = right._middle;
            rrow3 = right._bottom;

            lrow1 = left._top;
            lrow2 = left._middle;
            lrow3 = left._bottom;

            Vector128<float> r1 = Sse.Multiply(Sse.Shuffle(lrow1, lrow1, 0x00), rrow1);
            r1 = Sse.Add(r1, Sse.Multiply(Sse.Shuffle(lrow1, lrow1, 0x55), rrow2));
            r1 = Sse.Add(r1, Sse.Multiply(Sse.Shuffle(lrow1, lrow1, 0xAA), rrow3));

            Vector128<float> r2 = Sse.Multiply(Sse.Shuffle(lrow2, lrow2, 0x00), rrow1);
            r2 = Sse.Add(r2, Sse.Multiply(Sse.Shuffle(lrow2, lrow2, 0x55), rrow2));
            r2 = Sse.Add(r2, Sse.Multiply(Sse.Shuffle(lrow2, lrow2, 0xAA), rrow3));

            Vector128<float> r3 = Sse.Multiply(Sse.Shuffle(lrow3, lrow3, 0x00), rrow1);
            r3 = Sse.Add(r3, Sse.Multiply(Sse.Shuffle(lrow3, lrow3, 0x55), rrow2));
            r3 = Sse.Add(r3, Sse.Multiply(Sse.Shuffle(lrow3, lrow3, 0xAA), rrow3));

            result = new Matrix3(r1, r2, r3);
            return result;
        }

        result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31);
        result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32);
        result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33);

        result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31);
        result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32);
        result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33);

        result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31);
        result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32);
        result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Matrix3 left, Matrix3 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Matrix3 left, Matrix3 right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetElement(in Vector128<float> vec, int offset, float value)
    {
        ref float address = ref Unsafe.As<Vector128<float>, float>(ref Unsafe.AsRef(in vec));
        Unsafe.Add(ref address, offset) = value;
    }
}
