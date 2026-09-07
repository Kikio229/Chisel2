using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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

    private Vector128<float> _value;

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

    public float Length()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value).Sqrt();
        }

        return ((X * X) + (Y * Y) + (Z * Z)).Sqrt();
    }

    public float LengthSquared()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value);
        }

        return (X * X) + (Y * Y) + (Z * Z);
    }

    public float Barycenter(float amt0, float amt1)
    {
        return X + (Y - X) * amt0 + (Z - X) * amt1;
    }

    public float Distance(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> diff = Vector128.Subtract(_value, vec._value);
            return Vector128.Dot(diff, diff).Sqrt();
        }

        return ((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z)).Sqrt();
    }

    public float DistanceSquared(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> diff = Vector128.Subtract(_value, vec._value);
            return Vector128.Dot(diff, diff);
        }

        return (X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z);
    }

    public float DotProduct(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, vec._value);
        }

        return (X * vec.X) + (Y * vec.Y) + (Z * vec.Z);
    }

    public Vector3 CrossProduct(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<int> mask1, mask2;
            mask1 = Vector128.Create(1, 2, 0, 3);
            mask2 = Vector128.Create(2, 0, 1, 3);

            Vector128<float> ayzx, byzx, azxy, bzxy;
            ayzx = Vector128.Shuffle(_value, mask1);
            byzx = Vector128.Shuffle(vec._value, mask1);
            azxy = Vector128.Shuffle(_value, mask2);
            bzxy = Vector128.Shuffle(vec._value, mask2);

            return new Vector3(Vector128.Subtract(Vector128.Multiply(ayzx, bzxy), Vector128.Multiply(azxy, byzx)));
        }

        return new Vector3((Y * vec.Z) - (Z * vec.Y), (Z * vec.X) - (X * vec.Z), (X * vec.Y) - (Y * vec.X));
    }

    public Vector3 Negate()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Xor(_value, Vector128.Create(-0f)));
        }

        return new Vector3(-X, -Y, -Z);
    }

    public Vector3 TransformByMatrix(Matrix mat)
    {
        float x = (X * mat.M11) + (Y * mat.M21) + (Z * mat.M31) + mat.M41;
        float y = (X * mat.M12) + (Y * mat.M22) + (Z * mat.M32) + mat.M42;
        float z = (X * mat.M13) + (Y * mat.M23) + (Z * mat.M33) + mat.M43;
        return new Vector3(x, y, z);
    }

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

    public Vector3 Min(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Min(_value, vec._value));
        }

        return new Vector3(X.Min(vec.X), Y.Min(vec.Y), Z.Min(vec.Z));
    }

    public Vector3 Max(Vector3 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Max(_value, vec._value));
        }

        return new Vector3(X.Max(vec.X), Y.Max(vec.Y), Z.Max(vec.Z));
    }

    public Vector3 Normalize()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            float dot = Vector128.Dot(_value, _value);
            return (dot > 0.0f) ? new Vector3(Vector128.Divide(_value, Vector128.Create(dot.Sqrt()))) : Vector3.Zero;
        }

        float len = Length();
        return (len > 0.0f) ? new Vector3(X / len, Y / len, Z / len) : Vector3.Zero;
    }

    public Vector3 Lerp(Vector3 vec, float amount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Vector128.Subtract(vec._value, _value);
            return new Vector3(Vector128.Add(_value, Vector128.Multiply(tvec, diff)));
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
            "({0}, {1}, {2})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider),
            Z.ToString(format, formatProvider));
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3 other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.EqualsAll(_value, other._value);
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Add(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Subtract(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Multiply(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector3(Vector128.Divide(left._value, right._value));
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
