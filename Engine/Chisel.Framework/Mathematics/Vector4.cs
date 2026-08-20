using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Vector4 : IEquatable<Vector4>, IFormattable
{
    public float X
    {
        get => _value.GetElement(0);
        set => SetElement(_value, 0, value);
    }

    public float Y
    {
        get => _value.GetElement(1);
        set => SetElement(_value, 1, value);
    }

    public float Z
    {
        get => _value.GetElement(2);
        set => SetElement(_value, 2, value);
    }

    public float W
    {
        get => _value.GetElement(3);
        set => SetElement(_value, 3, value);
    }

    public Vector2 XY
    {
        get => new Vector2(X, Y);
    }

    public Vector3 XYZ
    {
        get => new Vector3(X, Y, Z);
    }

    public static Vector4 Zero => new Vector4(0f, 0f, 0f, 0f);
    public static Vector4 One => new Vector4(1f, 1f, 1f, 1f);

    private readonly Vector128<float> _value;

    public Vector4()
    {
        _value = Vector128.Create(0f, 0f, 0f, 0f);
    }

    public Vector4(float val)
    {
        _value = Vector128.Create(val, val, val, val);
    }

    public Vector4(float x, float y, float z, float w)
    {
        _value = Vector128.Create(x, y, z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector4(Vector128<float> value)
    {
        _value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> len = Sse.Sqrt(Sse41.DotProduct(_value, _value, 0xFF));
            return Vector128.ToScalar(len);
        }

        return ((X * X) + (Y * Y) + (Z * Z) + (W * W)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> len = Sse41.DotProduct(_value, _value, 0xFF);
            return Vector128.ToScalar(len);
        }

        return (X * X) + (Y * Y) + (Z * Z) + (W * W); 
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(Vector4 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse.Sqrt(Sse41.DotProduct(diff, diff, 0xFF));
            return Vector128.ToScalar(dist);
        }

        return ((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z) + (W - vec.W) * (W - vec.W)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceSquared(Vector4 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse41.DotProduct(diff, diff, 0xFF);
            return Vector128.ToScalar(dist);
        }

        return (X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z) + (W - vec.W) * (W - vec.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DotProduct(Vector4 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> dot = Sse41.DotProduct(_value, vec._value, 0xFF);
            return Vector128.ToScalar(dot);
        }

        return (X * vec.X) + (Y * vec.Y) + (Z * vec.Z) + (W * vec.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Negate()
    {
        if (Sse.IsSupported)
        {
            Vector128<float> mask = Vector128.Create(-0f);
            return new Vector4(Sse.Xor(_value, mask));
        }

        return new Vector4(-X, -Y, -Z, -W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 TransformByMatrix(Matrix mat)
    {
        float x = (X * mat.M11) + (Y * mat.M21) + (Z * mat.M31) + (W * mat.M41);
        float y = (X * mat.M12) + (Y * mat.M22) + (Z * mat.M32) + (W * mat.M42);
        float z = (X * mat.M13) + (Y * mat.M23) + (Z * mat.M33) + (W * mat.M43);
        float w = (X * mat.M14) + (Y * mat.M24) + (Z * mat.M34) + (W * mat.M44);
        return new Vector4(x, y, z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Min(Vector4 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Min(_value, vec._value));
        }

        return new Vector4(X.Min(vec.X), Y.Min(vec.Y), Z.Min(vec.Z), W.Min(vec.W));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Max(Vector4 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Max(_value, vec._value));
        }

        return new Vector4(X.Max(vec.X), Y.Max(vec.Y), Z.Max(vec.Z), W.Max(vec.W));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Normalize()
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> dot, ilen, half, threehalfs, ilenSqr;
            dot = Sse41.DotProduct(_value, _value, 0xFF);
            ilen = Sse.ReciprocalSqrt(dot);

            // Newton-Raphson refinement
            half = Vector128.Create(0.5f);
            threehalfs = Vector128.Create(1.5f);
            ilenSqr = Sse.Multiply(ilen, ilen);
            ilen = Sse.Multiply(ilen, Sse.Subtract(threehalfs, Sse.Multiply(Sse.Multiply(dot, ilenSqr), half)));

            return new Vector4(Sse.Multiply(_value, ilen));
        }

        float len = Length();
        return (len > 0.0f) ? new Vector4(X / len, Y / len, Z / len, W / len) : Vector4.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Lerp(Vector4 vec, float amount)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Sse.Subtract(vec._value, _value);
            return new Vector4(Sse.Add(_value, Sse.Multiply(tvec, diff)));
        }

        return new Vector4(
            X + amount * (vec.X - X),
            Y + amount * (vec.Y - Y),
            Z + amount * (vec.Z - Z),
            W + amount * (vec.W - W));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Vector4 ToNumerics()
    {
        return new System.Numerics.Vector4(X, Y, Z, W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string format)
    {
        return ToString(format, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(IFormatProvider formatProvider)
    {
        return ToString(null, formatProvider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(
            "({0}, {1}, {2}, {3})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider),
            Z.ToString(format, formatProvider),
            W.ToString(format, formatProvider));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return ToString(null, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        Hasher hasher = new Hasher();
        hasher.Add(X);
        hasher.Add(Y);
        hasher.Add(Z);
        hasher.Add(W);
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector4 other)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> equal = Sse.CompareEqual(_value, other._value);
            return Sse.MoveMask(equal) == 0xFF;
        }

        return (X == other.X) && (Y == other.Y) && (Z == other.Z) && (W == other.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Vector4)
        {
            return Equals((Vector4)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Add(Vector4 vec, float val)
    {
        return Add(vec, new Vector4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Add(Vector4 left, Vector4 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Add(left._value, right._value));
        }

        return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Subtract(Vector4 vec, float val)
    {
        return Subtract(vec, new Vector4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Subtract(Vector4 left, Vector4 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Subtract(left._value, right._value));
        }

        return new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Multiply(Vector4 vec, float val)
    {
        return Multiply(vec, new Vector4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Multiply(Vector4 left, Vector4 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Multiply(left._value, right._value));
        }

        return new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Divide(Vector4 vec, float val)
    {
        return Divide(vec, new Vector4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Divide(Vector4 left, Vector4 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector4(Sse.Divide(left._value, right._value));
        }

        return new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator +(Vector4 vec, float val)
    {
        return Add(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator +(Vector4 left, Vector4 right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 vec, float val)
    {
        return Subtract(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 left, Vector4 right)
    {
        return Subtract(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 vec, float val)
    {
        return Multiply(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 left, Vector4 right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 vec, float val)
    {
        return Divide(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 left, Vector4 right)
    {
        return Divide(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector4 left, Vector4 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector4 left, Vector4 right)
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
