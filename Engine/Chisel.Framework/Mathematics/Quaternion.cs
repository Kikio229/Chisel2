using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Chisel.Framework;

public struct Quaternion : IEquatable<Quaternion>, IFormattable
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

    public static Quaternion Zero => new Quaternion(0f, 0f, 0f, 0f);
    public static Quaternion Identity => new Quaternion(0f, 0f, 0f, 1f);

    private Vector128<float> _value;

    public Quaternion()
        : this(0f, 0f, 0f, 0f)
    {
        _value = Vector128.Create(0f, 0f, 0f, 0f);
    }

    public Quaternion(float val)
    {
        _value = Vector128.Create(val, val, val, val);
    }

    public Quaternion(float x, float y, float z, float w)
    {
        _value = Vector128.Create(x, y, z, w);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion(Vector128<float> value)
    {
        _value = value;
    }

    public float Length()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value).Sqrt();
        }

        return ((X * X) + (Y * Y) + (Z * Z) + (W * W)).Sqrt();
    }

    public float LengthSquared()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, _value);
        }

        return (X * X) + (Y * Y) + (Z * Z) + (W * W);
    }

    public float DotProduct(Quaternion quat)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.Dot(_value, quat._value);
        }

        return (X * quat.X) + (Y * quat.Y) + (Z * quat.Z) + (W * quat.W);
    }

    public Quaternion Normalize()
    {
        if (Vector128.IsHardwareAccelerated)
        {
            float dot = Vector128.Dot(_value, _value);
            return (dot > 0.0f) ? new Quaternion(Vector128.Divide(_value, Vector128.Create(dot.Sqrt()))) : Quaternion.Zero;
        }

        float len = Length();
        return (len > 0.0f) ? new Quaternion(X / len, Y / len, Z / len, W / len) : new Quaternion();
    }

    public Quaternion Lerp(Quaternion quat, float amount)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Vector128.Subtract(quat._value, _value);
            return new Quaternion(Vector128.Add(_value, Vector128.Multiply(tvec, diff)));
        }

        return new Quaternion(
            X + amount * (quat.X - X),
            Y + amount * (quat.Y - Y),
            Z + amount * (quat.Z - Z),
            W + amount * (quat.W - W)).Normalize();
    }

    public Quaternion Slerp(Quaternion quat, float amount)
    {
        float dot;

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> prod = Vector128.Multiply(_value, quat._value);
            dot = prod.GetElement(0) + prod.GetElement(1) + prod.GetElement(2) + prod.GetElement(3);

            if (dot < 0)
            {
                quat = new Quaternion(Vector128.Negate(quat._value));
                dot = -dot;
            }
        }
        else
        {
            dot = X * quat.X + Y * quat.Y + Z * quat.Z + W * quat.W;

            if (dot < 0)
            {
                quat = new Quaternion(-quat.X, -quat.Y, -quat.Z, -quat.W);
                dot = -dot;
            }
        }

        dot.Clamp(-1.0f, 1.0f);

        if (dot > 0.9995f)
        {
            return Lerp(quat, amount);
        }

        // float theta = dot.Acos();
        // float sinTheta = theta.Sin(theta);
        float sinTheta = (1.0f - dot * dot).Sqrt();
        float theta = sinTheta.Atan2(dot);

        if ((sinTheta).Abs() < MathUtilities.EpsilonF)
        {
            return Lerp(quat, amount);
        }

        float ratioA = ((1.0f - amount) * theta).Sin() / sinTheta;
        float ratioB = (amount * theta).Sin() / sinTheta;

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> ratioAVec, ratioBVec;
            ratioAVec = Vector128.Create(ratioA);
            ratioBVec = Vector128.Create(ratioB);
            return new Quaternion(Vector128.Add(Vector128.Multiply(_value, ratioAVec), Vector128.Multiply(quat._value, ratioBVec)));
        }
        else
        {
            return new Quaternion(
                (X * ratioA) + (quat.X * ratioB),
                (Y * ratioA) + (quat.Y * ratioB),
                (Z * ratioA) + (quat.Z * ratioB),
                (W * ratioA) + (quat.W * ratioB));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Quaternion ToNumerics()
    {
        return new System.Numerics.Quaternion(X, Y, Z, W);
    }

    public static Quaternion FromAxisAngle(Vector3 axis, float angle)
    {
        float half = angle * 0.5f;
        float sin = half.Sin();
        float cos = half.Cos();
        return new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos);
    }

    public static Quaternion FromRotationMatrix(Matrix matrix)
    {
        float sqrt, half;
        Quaternion result = Quaternion.Zero;
        float scale = matrix.M11 + matrix.M22 + matrix.M33;

        if (scale > 0.0f)
        {
            sqrt = (scale + 1.0f).Sqrt();
            result.W = sqrt * 0.5f;
            sqrt = 0.5f / sqrt;

            result.X = (matrix.M23 - matrix.M32) * sqrt;
            result.Y = (matrix.M31 - matrix.M13) * sqrt;
            result.Z = (matrix.M12 - matrix.M21) * sqrt;

            return result;
        }

        if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
        {
            sqrt = (1.0f + matrix.M11 - matrix.M22 - matrix.M33).Sqrt();
            half = 0.5f / sqrt;

            result.X = 0.5f * sqrt;
            result.Y = (matrix.M12 + matrix.M21) * half;
            result.Z = (matrix.M13 + matrix.M31) * half;
            result.W = (matrix.M23 - matrix.M32) * half;

            return result;
        }

        if (matrix.M22 > matrix.M33)
        {
            sqrt = (1.0f + matrix.M22 - matrix.M11 - matrix.M33).Sqrt();
            half = 0.5f / sqrt;

            result.X = (matrix.M21 + matrix.M12) * half;
            result.Y = 0.5f * sqrt;
            result.Z = (matrix.M32 + matrix.M23) * half;
            result.W = (matrix.M31 - matrix.M13) * half;

            return result;
        }

        sqrt = (1.0f + matrix.M33 - matrix.M11 - matrix.M22).Sqrt();
        half = 0.5f / sqrt;

        result.X = (matrix.M31 + matrix.M13) * half;
        result.Y = (matrix.M32 + matrix.M23) * half;
        result.Z = 0.5f * sqrt;
        result.W = (matrix.M12 - matrix.M21) * half;

        return result;
    }

    public static Quaternion FromYawPitchRoll(float yaw, float pitch, float roll)
    {
        float halfRoll = roll * 0.5f;
        float halfPitch = pitch * 0.5f;
        float halfYaw = yaw * 0.5f;

        float sinRoll = halfRoll.Sin();
        float cosRoll = halfRoll.Cos();
        float sinPitch = halfPitch.Sin();
        float cosPitch = halfPitch.Cos();
        float sinYaw = halfYaw.Sin();
        float cosYaw = halfYaw.Cos();

        return new Quaternion((cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll),
            (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll),
            (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll),
            (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll));
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
            "({0}, {1}, {2}, {3})",
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider),
            Z.ToString(format, formatProvider),
            W.ToString(format, formatProvider)
        );
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Quaternion other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector128.EqualsAll(_value, other._value);
        }

        return (X == other.X) && (Y == other.Y) && (Z == other.Z) && (W == other.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Quaternion)
        {
            return Equals((Quaternion)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Add(Quaternion left, Quaternion right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Quaternion(Vector128.Add(left._value, right._value));
        }

        return new Quaternion(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Multiply(Quaternion left, Quaternion right)
    {
        // Quaternion multiplication is real funky...
        // It's even funkier when simd is involed, so just trust that I somehow figured it out
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> lq, rq, lw, rw;

            lq = left._value;
            rq = right._value;
            lw = Vector128.Shuffle(lq, Vector128.Create(3));
            rw = Vector128.Shuffle(rq, Vector128.Create(3));

            Vector128<float> quat, cross, prod, dot;

            quat = Vector128.Add(Vector128.Multiply(lq, rw), Vector128.Multiply(lw, rq));
            cross = Vector128.Subtract(
                Vector128.Multiply(Vector128.Shuffle(lq, Vector128.Create(1, 2, 0, 3)), Vector128.Shuffle(rq, Vector128.Create(2, 0, 1, 3))),
                Vector128.Multiply(Vector128.Shuffle(lq, Vector128.Create(2, 0, 1, 3)), Vector128.Shuffle(rq, Vector128.Create(1, 2, 0, 3))));

            quat = Vector128.Add(quat, cross);
            prod = Vector128.Multiply(lq, rq);

            dot = Vector128.Add(prod, Vector128.Shuffle(prod, Vector128.Create(2, 3, 0, 1)));
            dot = Vector128.Add(dot, Vector128.Shuffle(prod, Vector128.Create(1)));
            dot = Vector128.Shuffle(dot, Vector128.Create(0));

            // I have make an int vector and then cast it to a float one
            // because floating-point errors are the bane of my existance
            Vector128<float> mask = Vector128.Create(0, 0, 0, -1).AsSingle();
            Vector128<float> scalar = Vector128.Subtract(Vector128.Multiply(lw, rw), dot);
            quat = Vector128.ConditionalSelect(mask, scalar, quat);

            return new Quaternion(quat);
        }

        return new Quaternion(
            (left.X * right.W) + (left.W * right.X) + (left.Y * right.Z) - (left.Z * right.Y),
            (left.Y * right.W) + (left.W * right.Y) + (left.Z * right.X) - (left.X * right.Z),
            (left.Z * right.W) + (left.W * right.Z) + (left.X * right.Y) - (left.Y * right.X),
            (left.W * right.W) - (left.X * right.X) - (left.Y * right.Y) - (left.Z * right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator +(Quaternion left, Quaternion right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion left, Quaternion right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Quaternion left, Quaternion right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Quaternion left, Quaternion right)
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
