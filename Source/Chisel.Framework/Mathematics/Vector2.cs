using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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

    public float Length()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value).Sqrt();
        }

        return ((X * X) + (Y * Y)).Sqrt();
    }

    public float LengthSquared()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value);
        }

        return (X * X) + (Y * Y);
    }
    public float Distance(Vector2 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> diff = Vector128.Subtract(_value, vec._value);
            return Vector128.Dot(diff, diff).Sqrt();
        }

        return ((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y)).Sqrt();
    }

    public float DistanceSquared(Vector2 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> diff = Vector128.Subtract(_value, vec._value);
            return Vector128.Dot(diff, diff);
        }

        return (X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y);
    }

    public float DotProduct(Vector2 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, vec._value);
        }

        return (X * vec.X) + (Y * vec.Y);
    }

    public Vector2 Negate()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Xor(_value, Vector128.Create(-0f)));
        }

        return new Vector2(-X, -Y);
    }

    public Vector2 TransformByMatrix(Matrix mat)
    {
        return new Vector2((X * mat.M11) + (Y * mat.M21) + mat.M31, (X * mat.M12) + (Y * mat.M22) + mat.M32);
    }

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

    public Vector2 Min(Vector2 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Min(_value, vec._value));
        }

        return new Vector2(X.Min(vec.X), Y.Min(vec.Y));
    }

    public Vector2 Max(Vector2 vec)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Max(_value, vec._value));
        }

        return new Vector2(X.Max(vec.X), Y.Max(vec.Y));
    }

    public Vector2 Normalize()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            float dot = Vector128.Dot(_value, _value);
            return (dot > 0.0f) ? new Vector2(Vector128.Divide(_value, Vector128.Create(dot.Sqrt()))) : Vector2.Zero;
        }

        float len = Length();
        return (len > 0.0f) ? new Vector2(X / len, Y / len) : Vector2.Zero;
    }

    public Vector2 Lerp(Vector2 vec, float amount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Vector128.Subtract(vec._value, _value);
            return new Vector2(Vector128.Add(_value, Vector128.Multiply(tvec, diff)));
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
            "({0}, {1})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider));
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2 other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.EqualsAll(_value, other._value);
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Add(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Subtract(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Multiply(left._value, right._value));
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
        if (Vector128.IsHardwareAccelerated)
        {
            return new Vector2(Vector128.Divide(left._value, right._value));
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