using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

    private readonly Vector128<float> _value;

    public Quaternion()
        : this(0f, 0f, 0f, 0f)
    {

    }

    public Quaternion(float val)
        : this(val, val, val, val)
    {

    }

    public Quaternion(float x, float y, float z, float w)
        : this(Vector128.Create(x, y, z, w))
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion(Vector128<float> value)
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
    public float DotProduct(Quaternion quat)
    {
        if (Sse41.IsSupported)
        {
            Vector128<float> dot = Sse41.DotProduct(_value, quat._value, 0xFF);
            return Vector128.ToScalar(dot);
        }

        return (X * quat.X) + (Y * quat.Y) + (Z * quat.Z) + (W * quat.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Normalize()
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

            return new Quaternion(Sse.Multiply(_value, ilen));
        }

        float len = Length();
        return (len > 0.0f) ? new Quaternion(X / len, Y / len, Z / len, W / len) : new Quaternion();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Lerp(Quaternion quat, float amount)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> tvec, diff;
            tvec = Vector128.Create(amount);
            diff = Sse.Subtract(quat._value, _value);
            return new Quaternion(Sse.Add(_value, Sse.Multiply(tvec, diff))).Normalize();
        }

        return new Quaternion(
            X + amount * (quat.X - X),
            Y + amount * (quat.Y - Y),
            Z + amount * (quat.Z - Z),
            W + amount * (quat.W - W)).Normalize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Slerp(Quaternion quat, float amount)
    {
        float dot;
        const float epsilon = 0.000001f;

        if (Sse.IsSupported)
        {
            Vector128<float> prod = Sse.Multiply(_value, quat._value);
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

        if ((sinTheta).Abs() < epsilon)
        {
            return Lerp(quat, amount);
        }

        float ratioA = ((1.0f - amount) * theta).Sin() / sinTheta;
        float ratioB = (amount * theta).Sin() / sinTheta;

        if (Sse.IsSupported)
        {
            Vector128<float> ratioAVec, ratioBVec;
            ratioAVec = Vector128.Create(ratioA);
            ratioBVec = Vector128.Create(ratioB);
            return new Quaternion(Sse.Add(Sse.Multiply(_value, ratioAVec), Sse.Multiply(quat._value, ratioBVec)));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion FromAxisAngle(Vector3 axis, float angle)
    {
        float half = angle * 0.5f;
        float sin = half.Sin();
        float cos = half.Cos();
        return new Quaternion(axis.X * sin, axis.Y * sin, axis.Z * sin, cos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            W.ToString(format, formatProvider)
        );
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
    public readonly bool Equals(Quaternion other)
    {
        if (Sse.IsSupported)
        {
            Vector128<float> equal = Sse.CompareEqual(_value, other._value);
            return Sse.MoveMask(equal) == 0xF;
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
        if (Sse.IsSupported)
        {
            return new Quaternion(Sse.Add(left._value, right._value));
        }

        return new Quaternion(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Subtract(Quaternion left, Quaternion right)
    {
        if (Sse.IsSupported)
        {
            return new Quaternion(Sse.Subtract(left._value, right._value));
        }

        return new Quaternion(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Multiply(Quaternion left, Quaternion right)
    {
        // TODO: SIMD optimizations?
        return new Quaternion(
            (left.X * right.W) + (left.W * right.X) + (left.Y * right.Z) - (left.Z * right.Y),
            (left.Y * right.W) + (left.W * right.Y) + (left.Z * right.X) - (left.X * right.Z),
            (left.Z * right.W) + (left.W * right.Z) + (left.X * right.Y) - (left.Y * right.X),
            (left.W * right.W) - (left.X * right.X) - (left.Y * right.Y) - (left.Z * right.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Divide(Quaternion left, Quaternion right)
    {
        // TODO: SIMD optimizations?
        float normSqr = (right.W * right.W) + (right.X * right.X) + (right.Y * right.Y) + (right.Z * right.Z);
        return new Quaternion(
            (left.X * right.W - left.W * right.X - left.Y * right.Z + left.Z * right.Y) * (1.0f / normSqr),
            (left.Y * right.W - left.W * right.Y - left.Z * right.X + left.X * right.Z) * (1.0f / normSqr),
            (left.Z * right.W - left.W * right.Z - left.X * right.Y + left.Y * right.X) * (1.0f / normSqr),
            (left.W * right.W + left.X * right.X + left.Y * right.Y + left.Z * right.Z) * (1.0f / normSqr));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator +(Quaternion left, Quaternion right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator -(Quaternion left, Quaternion right)
    {
        return Subtract(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion left, Quaternion right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator /(Quaternion left, Quaternion right)
    {
        return Divide(left, right);
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
