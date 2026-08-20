using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Vector2 : IEquatable<Vector2>, IFormattable
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

    public static Vector2 Zero => new Vector2(0f, 0f);
    public static Vector2 One => new Vector2(1f, 1f);
    public static Vector2 UnitX => new Vector2(1f, 0f);
    public static Vector2 UnitY => new Vector2(0f, 1f);

    private Vector128<float> _value;

    public Vector2()
    {
        _value = Vector128.Create(0f, 0f, 0f, 0f);
    }

    public Vector2(float val)
    {
        _value = Vector128.Create(val, val, 0f, 0f);
    }

    public Vector2(float x, float y)
    {
        _value = Vector128.Create(x, y, 0f, 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector2(Vector128<float> value)
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

        return ((X * X) + (Y * Y)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> len = Sse41.DotProduct(_value, _value, 0xFF);
            return Vector128.ToScalar(len);
        }

        return (X * X) + (Y * Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(Vector2 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse.Sqrt(Sse41.DotProduct(diff, diff, 0xFF));
            return Vector128.ToScalar(dist);
        }

        return ((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceSquared(Vector2 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse41.DotProduct(diff, diff, 0xFF);
            return Vector128.ToScalar(dist);
        }

        return (X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DotProduct(Vector2 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> dot = Sse41.DotProduct(_value, vec._value, 0xFF);
            return Vector128.ToScalar(dot);
        }

        return (X * vec.X) + (Y * vec.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Negate()
    {
        if (Sse.IsSupported)
        {
            Vector128<float> mask = Vector128.Create(-0f);
            return new Vector2(Sse.Xor(_value, mask));
        }

        return new Vector2(-X, -Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TransformByMatrix(Matrix mat)
    {
        return new Vector2((X * mat.M11) + (Y * mat.M21) + mat.M41, (X * mat.M12) + (Y * mat.M22) + mat.M42);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TransformByQuaternion(Quaternion quat)
    {
        Vector3 rot1 = new Vector3(quat.X + quat.X, quat.Y + quat.Y, quat.Z + quat.Z);
        Vector3 rot2 = new Vector3(quat.X, quat.X, quat.W);
        Vector3 rot3 = new Vector3(1f, quat.Y, quat.Z);
        Vector3 rot4 = rot1 * rot2;
        Vector3 rot5 = rot1 * rot3;

        return new Vector2(
           (float)(X * (1.0f - rot5.Y - rot5.Z) + Y * (rot4.Y - rot4.Z)),
           (float)(X * (rot4.Y + rot4.Z) + Y * (1.0f - rot4.X - rot5.Z)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Min(Vector2 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Min(_value, vec._value));
        }

        return new Vector2(X.Min(vec.X), Y.Min(vec.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Max(Vector2 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Max(_value, vec._value));
        }

        return new Vector2(X.Max(vec.X), Y.Max(vec.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Normalize()
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

            return new Vector2(Sse.Multiply(_value, ilen));
        }

        float len = Length();
        return (len > 0.0f) ? new Vector2(X / len, Y / len) : Vector2.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Lerp(Vector2 vec, float amount)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Sse.Subtract(vec._value, _value);
            return new Vector2(Sse.Add(_value, Sse.Multiply(tvec, diff)));
        }

        return new Vector2(
            X + amount * (vec.X - X),
            Y + amount * (vec.Y - Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Vector2 ToNumerics()
    {
        return new System.Numerics.Vector2(X, Y);
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
            "({0}, {1})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider));
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
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2 other)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> equal = Sse.CompareEqual(_value, other._value);
            return Sse.MoveMask(equal) == 0xFF;
        }

        return (X == other.X) && (Y == other.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Vector2)
        {
            return Equals((Vector2)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Add(Vector2 vec, float val)
    {
        return Add(vec, new Vector2(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Add(Vector2 left, Vector2 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Add(left._value, right._value));
        }

        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Subtract(Vector2 vec, float val)
    {
        return Subtract(vec, new Vector2(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Subtract(Vector2 left, Vector2 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Subtract(left._value, right._value));
        }

        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Multiply(Vector2 vec, float val)
    {
        return Multiply(vec, new Vector2(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Multiply(Vector2 left, Vector2 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Multiply(left._value, right._value));
        }

        return new Vector2(left.X * right.X, left.Y * right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Divide(Vector2 vec, float val)
    {
        return Divide(vec, new Vector2(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Divide(Vector2 left, Vector2 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector2(Sse.Divide(left._value, right._value));
        }

        return new Vector2(left.X / right.X, left.Y / right.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator +(Vector2 vec, float val)
    {
        return Add(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator -(Vector2 vec, float val)
    {
        return Subtract(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return Subtract(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 vec, float val)
    {
        return Multiply(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 left, Vector2 right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2 vec, float val)
    {
        return Divide(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2 left, Vector2 right)
    {
        return Divide(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2 left, Vector2 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2 left, Vector2 right)
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