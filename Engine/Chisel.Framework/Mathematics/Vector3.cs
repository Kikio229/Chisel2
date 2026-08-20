using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Vector3 : IEquatable<Vector3>, IFormattable
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

    public Vector2 XY
    {
        get => new Vector2(X, Y);
    }

    public static Vector3 Zero => new Vector3(0f, 0f, 0f);
    public static Vector3 One => new Vector3(1f, 1f, 1f);
    public static Vector3 UnitX => new Vector3(1f, 0f, 0f);
    public static Vector3 UnitY => new Vector3(0f, 1f, 0f);
    public static Vector3 UnitZ => new Vector3(0f, 0f, 1f);

    private readonly Vector128<float> _value;

    public Vector3()
    {
        _value = Vector128.Create(0f, 0f, 0f, 0f);
    }

    public Vector3(float val)
    {
        _value = Vector128.Create(val, val, val, 0f);
    }

    public Vector3(float x, float y, float z)
    {
        _value = Vector128.Create(x, y, z, 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3(Vector128<float> value)
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

        return ((X * X) + (Y * Y) + (Z * Z)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> len = Sse41.DotProduct(_value, _value, 0xFF);
            return Vector128.ToScalar(len);
        }

        return (X * X) + (Y * Y) + (Z * Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Barycenter(float amt0, float amt1)
    {
        return X + (Y - X) * amt0 + (Z - X) * amt1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Distance(Vector3 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse.Sqrt(Sse41.DotProduct(diff, diff, 0xFF));
            return Vector128.ToScalar(dist);
        }

        return ((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z)).Sqrt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceSquared(Vector3 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> diff, dist;
            diff = Sse.Subtract(_value, vec._value);
            dist = Sse41.DotProduct(diff, diff, 0xFF);
            return Vector128.ToScalar(dist);
        }

        return (X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DotProduct(Vector3 vec)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> dot = Sse41.DotProduct(_value, vec._value, 0xFF);
            return Vector128.ToScalar(dot);
        }

        return (X * vec.X) + (Y * vec.Y) + (Z * vec.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 CrossProduct(Vector3 vec)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> ayzx, byzx, azxy, bzxy;
            ayzx = Sse.Shuffle(_value, _value, 0xC9);
            byzx = Sse.Shuffle(vec._value, vec._value, 0xC9);
            azxy = Sse.Shuffle(_value, _value, 0xD2);
            bzxy = Sse.Shuffle(vec._value, vec._value, 0xD2);
            return new Vector3(Sse.Subtract(Sse.Multiply(ayzx, bzxy), Sse.Multiply(azxy, byzx)));
        }

        return new Vector3((Y * vec.Z) - (Z * vec.Y), (Z * vec.X) - (X * vec.Z), (X * vec.Y) - (Y * vec.X));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Negate()
    {
        if (Sse.IsSupported)
        {
            Vector128<float> mask = Vector128.Create(-0f);
            return new Vector3(Sse.Xor(_value, mask));
        }

        return new Vector3(-X, -Y, -Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformByMatrix(Matrix mat)
    {
        float x = (X * mat.M11) + (Y * mat.M21) + (Z * mat.M31) + mat.M41;
        float y = (X * mat.M12) + (Y * mat.M22) + (Z * mat.M32) + mat.M42;
        float z = (X * mat.M13) + (Y * mat.M23) + (Z * mat.M33) + mat.M43;
        return new Vector3(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformByQuaternion(Quaternion quat)
    {
        float x = 2 * (quat.Y * Z - quat.Z * Y);
        float y = 2 * (quat.Z * X - quat.X * Z);
        float z = 2 * (quat.X * Y - quat.Y * X);

        return new Vector3(
            X + x * quat.W + (quat.Y * z - quat.Z * y),
            Y + y * quat.W + (quat.Z * x - quat.X * z),
            Z + z * quat.W + (quat.X * y - quat.Y * x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Min(Vector3 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Min(_value, vec._value));
        }

        return new Vector3(X.Min(vec.X), Y.Min(vec.Y), Z.Min(vec.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Max(Vector3 vec)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Max(_value, vec._value));
        }

        return new Vector3(X.Max(vec.X), Y.Max(vec.Y), Z.Max(vec.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Normalize()
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

            return new Vector3(Sse.Multiply(_value, ilen));
        }

        float len = Length();
        return (len > 0.0f) ? new Vector3(X / len, Y / len, Z / len) : Vector3.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Lerp(Vector3 vec, float amount)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Sse.Subtract(vec._value, _value);
            return new Vector3(Sse.Add(_value, Sse.Multiply(tvec, diff)));
        }

        return new Vector3(
            X + amount * (vec.X - X),
            Y + amount * (vec.Y - Y),
            Z + amount * (vec.Z - Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Vector3 ToNumerics()
    {
        return new System.Numerics.Vector3(X, Y, Z);
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
            "({0}, {1}, {2})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider),
            Z.ToString(format, formatProvider));
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
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3 other)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> equal = Sse.CompareEqual(_value, other._value);
            return Sse.MoveMask(equal) == 0xFF;
        }

        return (X == other.X) && (Y == other.Y) && (Z == other.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Vector3)
        {
            return Equals((Vector3)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Add(Vector3 vec, float val)
    {
        return Add(vec, new Vector3(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Add(Vector3 left, Vector3 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Add(left._value, right._value));
        }

        return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Subtract(Vector3 vec, float val)
    {
        return Subtract(vec, new Vector3(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Subtract(Vector3 left, Vector3 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Subtract(left._value, right._value));
        }

        return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(Vector3 vec, float val)
    {
        return Multiply(vec, new Vector3(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Multiply(Vector3 left, Vector3 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Multiply(left._value, right._value));
        }

        return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Divide(Vector3 vec, float val)
    {
        return Divide(vec, new Vector3(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Divide(Vector3 left, Vector3 right)
    {
        if (Sse.IsSupported)
        {
            return new Vector3(Sse.Divide(left._value, right._value));
        }

        return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 vec, float val)
    {
        return Add(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 vec, float val)
    {
        return Subtract(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return Subtract(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 vec, float val)
    {
        return Multiply(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 left, Vector3 right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 vec, float val)
    {
        return Divide(vec, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 left, Vector3 right)
    {
        return Divide(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3 left, Vector3 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3 left, Vector3 right)
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
