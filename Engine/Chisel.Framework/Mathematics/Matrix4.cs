using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Matrix4 : IEquatable<Matrix4>, IFormattable
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

    public float M14
    {
        get => _top.GetElement(3);
        set => SetElement(_top, 3, value);
    }

    // Row 2

    public float M21
    {
        get => _top.GetElement(4);
        set => SetElement(_top, 4, value);
    }

    public float M22
    {
        get => _top.GetElement(5);
        set => SetElement(_top, 5, value);
    }

    public float M23
    {
        get => _top.GetElement(6);
        set => SetElement(_top, 6, value);
    }

    public float M24
    {
        get => _top.GetElement(7);
        set => SetElement(_top, 7, value);
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

    public float M34
    {
        get => _bottom.GetElement(3);
        set => SetElement(_bottom, 3, value);
    }

    // Row 4

    public float M41
    {
        get => _bottom.GetElement(4);
        set => SetElement(_bottom, 4, value);
    }

    public float M42
    {
        get => _bottom.GetElement(5);
        set => SetElement(_bottom, 5, value);
    }

    public float M43
    {
        get => _bottom.GetElement(6);
        set => SetElement(_bottom, 6, value);
    }

    public float M44
    {
        get => _bottom.GetElement(7);
        set => SetElement(_bottom, 7, value);
    }

    public Vector3 Forward
    {
        get
        {
            return new Vector3(-M31, -M32, -M33);
        }
        set
        {
            M31 = -value.X;
            M32 = -value.Y;
            M33 = -value.Z;
        }
    }

    public Vector3 Backward
    {
        get
        {
            return new Vector3(M31, M32, M33);
        }
        set
        {
            M31 = value.X;
            M32 = value.Y;
            M33 = value.Z;
        }
    }

    public Vector3 Right
    {
        get
        {
            return new Vector3(M11, M12, M13);
        }
        set
        {
            M11 = value.X;
            M12 = value.Y;
            M13 = value.Z;
        }
    }

    public Vector3 Left
    {
        get
        {
            return new Vector3(-M11, -M12, -M13);
        }
        set
        {
            M11 = -value.X;
            M12 = -value.Y;
            M13 = -value.Z;
        }
    }

    public Vector3 Up
    {
        get
        {
            return new Vector3(M21, M22, M23);
        }
        set
        {
            M21 = value.X;
            M22 = value.Y;
            M23 = value.Z;
        }
    }

    public Vector3 Down
    {
        get
        {
            return new Vector3(-M21, -M22, -M23);
        }
        set
        {
            M21 = -value.X;
            M22 = -value.Y;
            M23 = -value.Z;
        }
    }
     
    public static Matrix4 Zero => new Matrix4(0f);
    public static Matrix4 One => new Matrix4(1f);
    public static Matrix4 Identity = new Matrix4(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

    private Vector256<float> _top, _bottom;

    public Matrix4()
    {
        _top = Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        _bottom = Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
    }

    public Matrix4(float val)
    {
        _top = Vector256.Create(val, val, val, val, val, val, val, val);
        _bottom = Vector256.Create(val, val, val, val, val, val, val, val);
    }

    public Matrix4(Vector4 vec1, Vector4 vec2, Vector4 vec3, Vector4 vec4) 
    {
        _top = Vector256.Create(vec1.X, vec1.Y, vec1.Z, vec1.W, vec2.X, vec2.Y, vec2.Z, vec2.W);
        _bottom = Vector256.Create(vec2.X, vec3.Y, vec3.Z, vec3.W, vec4.X, vec4.Y, vec4.Z, vec4.W);
    }

    public Matrix4(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
    {
        _top = Vector256.Create(m11, m12, m13, m14, m21, m22, m23, m24);
        _bottom = Vector256.Create(m31, m32, m33, m34, m41, m42, m43, m44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Matrix4(Vector256<float> top, Vector256<float> bottom)
    {
        _top = top;
        _bottom = bottom;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 Invert()
    {
        Matrix4 result = Matrix4.Zero;

        float n1, n2, n3, n4, n5, n6, n7, n8, n9, n10;
        float n11, n12, n13, n14, n15, n16, n17, n18, n19, n20;
        float n21, n22, n23;

        // TODO: Some SIMD optimizations if possible?
        if (MathUtilities.X86SimdSupported)
        {

        }

        n1 = (float)(M33 * M44 - M34 * M43);
        n2 = (float)(M32 * M44 - M34 * M42);
        n3 = (float)(M32 * M43 - M33 * M42);
        n4 = (float)(M31 * M44 - M34 * M41);
        n5 = (float)(M31 * M43 - M33 * M41);
        n6 = (float)(M31 * M42 - M32 * M41);
        n7 = (float)(M22 * n1 - M23 * n2 + M24 * n3);
        n8 = (float)-(M21 * n1 - M23 * n4 + M24 * n5);
        n9 = (float)(M21 * n2 - M22 * n4 + M24 * n6);
        n10 = (float)-(M21 * n3 - M22 * n5 + M23 * n6);
        n11 = (float)(1.0f / (M11 * n7 + M12 * n8 + M13 * n9 + M14 * n10));

        result.M11 = n7 * n11;
        result.M21 = n8 * n11;
        result.M31 = n9 * n11;
        result.M41 = n10 * n11;

        result.M12 = (float)-(M12 * n1 - M13 * n2 + M14 * n3) * n11;
        result.M22 = (float)(M11 * n1 - M13 * n4 + M14 * n5) * n11;
        result.M32 = (float)-(M11 * n2 - M12 * n4 + M14 * n6) * n11;
        result.M42 = (float)(M11 * n3 - M12 * n5 + M13 * n6) * n11;

        n12 = (float)(M23 * M44 - M24 * M43);
        n13 = (float)(M22 * M44 - M24 * M42);
        n14 = (float)(M22 * M43 - M23 * M42);
        n15 = (float)(M21 * M44 - M24 * M41);
        n16 = (float)(M21 * M43 - M23 * M41);
        n17 = (float)(M21 * M42 - M22 * M41);

        result.M13 = (float)(M12 * n12 - M13 * n13 + M14 * n14) * n11;
        result.M23 = (float)-(M11 * n12 - M13 * n15 + M14 * n16) * n11;
        result.M33 = (float)(M11 * n13 - M12 * n15 + M14 * n17) * n11;
        result.M43 = (float)-(M11 * n14 - M12 * n16 + M13 * n17) * n11;

        n18 = (float)(M23 * M34 - M24 * M33);
        n19 = (float)(M22 * M34 - M24 * M32);
        n20 = (float)(M22 * M33 - M23 * M32);
        n21 = (float)(M21 * M34 - M24 * M31);
        n22 = (float)(M21 * M33 - M23 * M31);
        n23 = (float)(M21 * M32 - M22 * M31);

        result.M14 = (float)-(M12 * n18 - M13 * n19 + M14 * n20) * n11;
        result.M24 = (float)(M11 * n18 - M13 * n21 + M14 * n22) * n11;
        result.M34 = (float)-(M11 * n19 - M12 * n21 + M14 * n23) * n11;
        result.M44 = (float)(M11 * n20 - M12 * n22 + M13 * n23) * n11;

        return result;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 Negate()
    {
        Matrix4 result = Matrix4.Zero;

        if (MathUtilities.X86SimdSupported)
        {
            Vector256<float> mask = Vector256.Create(-0f);
            result = new Matrix4(Avx.Xor(_top, mask), Avx.Xor(_bottom, mask));
            return result;
        }

        result.M11 = -M11;
        result.M12 = -M12;
        result.M13 = -M13;
        result.M14 = -M14;

        result.M21 = -M21;
        result.M22 = -M22;
        result.M23 = -M23;
        result.M24 = -M24;

        result.M31 = -M31;
        result.M32 = -M32;
        result.M33 = -M33;
        result.M34 = -M34;

        result.M41 = -M41;
        result.M42 = -M42;
        result.M43 = -M43;
        result.M44 = -M44;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 Transpose()
    {
        Matrix4 result = Matrix4.Zero;

        if (MathUtilities.X86SimdSupported)
        {
            Vector256<float> r13, r24, v13, v24, c13, c24;

            r13 = Avx.Permute2x128(_top, _bottom, 0x20);
            r24 = Avx.Permute2x128(_top, _bottom, 0x31);

            Vector256<float> t1, t2;
            t1 = Avx.UnpackLow(r13, r24);
            t2 = Avx.UnpackHigh(r13, r24);
            v13 = Avx.Permute2x128(t1, t2, 0x20);
            v24 = Avx.Permute2x128(t1, t2, 0x31);

            c13 = Avx.Shuffle(v13, v24, 0x44);
            c24 = Avx.Shuffle(v13, v24, 0xEE);

            return new Matrix4(Avx.Permute2x128(c13, c24, 0x20), Avx.Permute2x128(c13, c24, 0x31));
        }

        result.M11 = M11;
        result.M12 = M21;
        result.M13 = M31;
        result.M14 = M41;

        result.M21 = M12;
        result.M22 = M22;
        result.M23 = M32;
        result.M24 = M42;

        result.M31 = M13;
        result.M32 = M23;
        result.M33 = M33;
        result.M34 = M43;

        result.M41 = M14;
        result.M42 = M24;
        result.M43 = M34;
        result.M44 = M44;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Matrix4x4 ToNumerics()
    {
        return new System.Numerics.Matrix4x4(
            M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromRotationX(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M22 = val1;
        result.M23 = val2;
        result.M32 = -val2;
        result.M33 = val1;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromRotationY(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M11 = val1;
        result.M13 = -val2;
        result.M31 = val2;
        result.M33 = val1;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromRotationZ(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M11 = val1;
        result.M12 = val2;
        result.M21 = -val2;
        result.M22 = val1;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromAxisAngle(Vector3 axis, float angle)
    {
        Matrix4 result = Matrix4.Zero;
        float n1, n2, n3, n4, n5, n6, n7, n8;

        float x = axis.X;
        float y = axis.Y;
        float z = axis.Z;

        n1 = angle.Cos();
        n2 = angle.Sin();
        n3 = x * x;
        n4 = y * y;
        n5 = z * z;
        n6 = x * y;
        n7 = x * z;
        n8 = y * z;

        result.M11 = n3 + (n1 * (1f - n3));
        result.M12 = (n6 - (n1 * n6)) + (n2 * z);
        result.M13 = (n7 - (n1 * n7)) - (n2 * y);

        result.M21 = (n6 - (n1 * n6)) - (n2 * z);
        result.M22 = n4 + (n1 * (1f - n4));
        result.M23 = (n8 - (n1 * n8)) + (n2 * x);

        result.M31 = (n7 - (n1 * n7)) + (n2 * y);
        result.M32 = (n8 - (n1 * n8)) - (n2 * x);
        result.M33 = n5 + (n1 * (1f - n5));

        result.M44 = 1f;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromQuaternion(Quaternion quaternion)
    {
        Matrix4 result = Matrix4.Zero;
        float n1, n2, n3, n4, n5, n6, n7, n8, n9;

        n7 = quaternion.Z * quaternion.Z;
        n1 = quaternion.X * quaternion.W;
        n2 = quaternion.Y * quaternion.Z;
        n3 = quaternion.Y * quaternion.W;
        n4 = quaternion.Z * quaternion.X;
        n5 = quaternion.Z * quaternion.W;
        n6 = quaternion.X * quaternion.Y;
        n8 = quaternion.Y * quaternion.Y;
        n9 = quaternion.X * quaternion.X;

        result.M11 = 1f - (2f * (n8 + n7));
        result.M12 = 2f * (n6 + n5);
        result.M13 = 2f * (n4 - n3);

        result.M21 = 2f * (n6 - n5);
        result.M22 = 1f - (2f * (n7 + n9));
        result.M23 = 2f * (n2 + n1);

        result.M31 = 2f * (n4 + n3);
        result.M32 = 2f * (n2 - n1);
        result.M33 = 1f - (2f * (n8 + n9));

        result.M44 = 1f;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromYawPitchRoll(float yaw, float pitch, float roll)
    {
        return FromQuaternion(Quaternion.FromYawPitchRoll(yaw, pitch, roll));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromTranslation(Vector3 translate)
    {
        Matrix4 result = Matrix4.Zero;
        result.M11 = 1f;
        result.M22 = 1f;
        result.M33 = 1f;
        result.M41 = translate.X;
        result.M42 = translate.Y;
        result.M43 = translate.Z;
        result.M44 = 1f;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromScale(Vector3 scale)
    {
        Matrix4 result = Matrix4.Zero;
        result.M11 = scale.X;
        result.M22 = scale.Y;
        result.M33 = scale.Z;
        result.M44 = 1f;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromLookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Matrix4 result = Matrix4.Zero;

        Vector3 vecA = (position - target).Normalize();
        Vector3 vecB = (up.CrossProduct(vecA)).Normalize();
        Vector3 vecC = vecA.CrossProduct(vecB);
        result.M11 = vecB.X;
        result.M12 = vecC.X;
        result.M13 = vecA.X;
        result.M21 = vecB.Y;
        result.M22 = vecC.Y;
        result.M23 = vecA.Y;
        result.M31 = vecB.Z;
        result.M32 = vecC.Z;
        result.M33 = vecA.Z;
        result.M41 = -vecB.DotProduct(position);
        result.M42 = -vecC.DotProduct(position);
        result.M43 = -vecA.DotProduct(position);
        result.M44 = 1f;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromOrthographic(float left, float right, float bottom, float top, float near, float far)
    {
        Matrix4 result = Matrix4.Zero;

        result.M11 = (float)(2.0 / (right - left));
        result.M22 = (float)(2.0 / (top - bottom));
        result.M33 = (float)(1.0 / (near - far));
        result.M41 = (float)((left + right) / (left - right));
        result.M42 = (float)((top + bottom) / (bottom - top));
        result.M43 = (float)(near / (near - far));
        result.M44 = 1.0f;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromPerspective(float left, float right, float top, float bottom, float near, float far)
    {
        Matrix4 result = Matrix4.Zero;

        if (near <= 0f || far <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(near), $"Near or far plane distance cannot be less than 0!");
        }

        if (near >= far)
        {
            throw new ArgumentOutOfRangeException(nameof(near), $"Near plane distance cannot be the same (or greater) as far plane!");
        }

        result.M11 = (2f * near) / (right - left);
        result.M22 = (2f * near) / (top - bottom);
        result.M31 = (left + right) / (right - left);
        result.M32 = (top + bottom) / (top - bottom);
        result.M33 = far / (near - far);
        result.M34 = -1f;
        result.M43 = (near * far) / (near - far);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 FromPerspectiveFov(float fovy, float ratio, float near, float far)
    {
        Matrix4 result = Matrix4.Zero;

        if (near <= 0f || far <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(near), $"Near or far plane distance cannot be less than 0!");
        }

        if ((fovy <= 0f) || (fovy >= MathUtilities.Pi))
        {
            throw new ArgumentOutOfRangeException(nameof(near), $"Field of view cannot be 0 or greater than PI!");
        }

        if (near >= far)
        {
            throw new ArgumentOutOfRangeException(nameof(near), $"Near plane distance cannot be the same (or greater) as far plane!");
        }

        float yScale = 1.0f / (float)Math.Tan(fovy * 0.5f);
        float xScale = yScale / ratio;
        float negFarRange = float.IsPositiveInfinity(far) ? -1.0f : far / (near - far);

        result.M11 = xScale;
        result.M22 = yScale;
        result.M33 = negFarRange;
        result.M34 = -1.0f;
        result.M43 = near * negFarRange;

        return result;
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
            "({0}, {1}, {2}, {3},\n {4}, {5}, {6}, {7},\n {8}, {9}, {10}, {11},\n {12}, {13}, {14}, {15})",
            M11.ToString(format, formatProvider),
            M12.ToString(format, formatProvider),
            M13.ToString(format, formatProvider),
            M14.ToString(format, formatProvider),
            M21.ToString(format, formatProvider),
            M22.ToString(format, formatProvider),
            M23.ToString(format, formatProvider),
            M24.ToString(format, formatProvider),
            M31.ToString(format, formatProvider),
            M32.ToString(format, formatProvider),
            M33.ToString(format, formatProvider),
            M34.ToString(format, formatProvider),
            M41.ToString(format, formatProvider),
            M42.ToString(format, formatProvider),
            M43.ToString(format, formatProvider),
            M44.ToString(format, formatProvider));
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
        hasher.Add(M11);
        hasher.Add(M12);
        hasher.Add(M13);
        hasher.Add(M14);
        hasher.Add(M21);
        hasher.Add(M22);
        hasher.Add(M23);
        hasher.Add(M24);
        hasher.Add(M31);
        hasher.Add(M32);
        hasher.Add(M33);
        hasher.Add(M34);
        hasher.Add(M41);
        hasher.Add(M42);
        hasher.Add(M43);
        hasher.Add(M44);
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Matrix4 other)
    {
        if (MathUtilities.X86SimdSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Compare(_top, other._top, FloatComparisonMode.OrderedEqualNonSignaling);
            bottom = Avx.Compare(_bottom, other._bottom, FloatComparisonMode.OrderedEqualNonSignaling);
            return (Avx.MoveMask(top) == 0xFF) && (Avx.MoveMask(bottom) == 0xFF);
        }
    
        return (M11 == other.M11) && (M12 == other.M12) && (M13 == other.M13) && (M14 == other.M14) &&
            (M21 == other.M21) && (M22 == other.M22) && (M23 == other.M23) && (M24 == other.M24) &&
            (M31 == other.M31) && (M32 == other.M32) && (M33 == other.M33) && (M34 == other.M34) &&
            (M41 == other.M41) && (M42 == other.M42) && (M43 == other.M43) && (M44 == other.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Matrix4)
        {
            return Equals((Matrix4)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 Add(Matrix4 mat, float val)
    {
        return Add(mat, new Matrix4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 Add(Matrix4 left, Matrix4 right)
    {
        if (MathUtilities.X86SimdSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Add(left._top, right._top);
            bottom = Avx.Add(left._bottom, right._bottom);
            return new Matrix4(top, bottom);
        }

        return new Matrix4(left.M11 + right.M11,
            left.M12 + right.M12,
            left.M13 + right.M13,
            left.M14 + right.M14,
            left.M21 + right.M21,
            left.M22 + right.M22,
            left.M23 + right.M23,
            left.M24 + right.M24,
            left.M31 + right.M31,
            left.M32 + right.M32,
            left.M33 + right.M33,
            left.M34 + right.M34,
            left.M41 + right.M41,
            left.M42 + right.M42,
            left.M43 + right.M43,
            left.M44 + right.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 Multiply(Matrix4 mat, float val)
    {
        return Multiply(mat, new Matrix4(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 Multiply(Matrix4 left, Matrix4 right)
    {
        Matrix4 result = Matrix4.Zero;

        if (MathUtilities.X86SimdSupported)
        {
            Vector256<float> r1, r2, r3, r4;
            r1 = Avx.Permute2x128(right._top, right._top, 0x00);
            r2 = Avx.Permute2x128(right._top, right._top, 0x11);
            r3 = Avx.Permute2x128(right._bottom, right._bottom, 0x00);
            r4 = Avx.Permute2x128(right._bottom, right._bottom, 0x11);

            Vector256<float> top = Avx.Multiply(Avx.Shuffle(left._top, left._top, 0x00), r1);
            top = Avx.Add(top, Avx.Multiply(Avx.Shuffle(left._top, left._top, 0x55), r2));
            top = Avx.Add(top, Avx.Multiply(Avx.Shuffle(left._top, left._top, 0xAA), r3));
            top = Avx.Add(top, Avx.Multiply(Avx.Shuffle(left._top, left._top, 0xFF), r4));

            Vector256<float> bottom = Avx.Multiply(Avx.Shuffle(left._bottom, left._bottom, 0x00), r1);
            bottom = Avx.Add(bottom, Avx.Multiply(Avx.Shuffle(left._bottom, left._bottom, 0x55), r2));
            bottom = Avx.Add(bottom, Avx.Multiply(Avx.Shuffle(left._bottom, left._bottom, 0xAA), r3));
            bottom = Avx.Add(bottom, Avx.Multiply(Avx.Shuffle(left._bottom, left._bottom, 0xFF), r4));

            result = new Matrix4(top, bottom);
            return result;
        }
        
        result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21) + (left.M13 * right.M31) + (left.M14 * right.M41);
        result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22) + (left.M13 * right.M32) + (left.M14 * right.M42);
        result.M13 = (left.M11 * right.M13) + (left.M12 * right.M23) + (left.M13 * right.M33) + (left.M14 * right.M43);
        result.M14 = (left.M11 * right.M14) + (left.M12 * right.M24) + (left.M13 * right.M34) + (left.M14 * right.M44);

        result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21) + (left.M23 * right.M31) + (left.M24 * right.M41);
        result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22) + (left.M23 * right.M32) + (left.M24 * right.M42);
        result.M23 = (left.M21 * right.M13) + (left.M22 * right.M23) + (left.M23 * right.M33) + (left.M24 * right.M43);
        result.M24 = (left.M21 * right.M14) + (left.M22 * right.M24) + (left.M23 * right.M34) + (left.M24 * right.M44);

        result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + (left.M33 * right.M31) + (left.M34 * right.M41);
        result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + (left.M33 * right.M32) + (left.M34 * right.M42);
        result.M33 = (left.M31 * right.M13) + (left.M32 * right.M23) + (left.M33 * right.M33) + (left.M34 * right.M43);
        result.M34 = (left.M31 * right.M14) + (left.M32 * right.M24) + (left.M33 * right.M34) + (left.M34 * right.M44);

        result.M41 = (left.M41 * right.M11) + (left.M42 * right.M21) + (left.M43 * right.M31) + (left.M44 * right.M41);
        result.M42 = (left.M41 * right.M12) + (left.M42 * right.M22) + (left.M43 * right.M32) + (left.M44 * right.M42);
        result.M43 = (left.M41 * right.M13) + (left.M42 * right.M23) + (left.M43 * right.M33) + (left.M44 * right.M43);
        result.M44 = (left.M41 * right.M14) + (left.M42 * right.M24) + (left.M43 * right.M34) + (left.M44 * right.M44);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 operator +(Matrix4 mat, float val)
    {
        return Add(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 operator +(Matrix4 left, Matrix4 right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 operator *(Matrix4 mat, float val)
    {
        return Multiply(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4 operator *(Matrix4 left, Matrix4 right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Matrix4 left, Matrix4 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Matrix4 left, Matrix4 right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetElement(in Vector256<float> vec, int offset, float value)
    {
        ref float address = ref Unsafe.As<Vector256<float>, float>(ref Unsafe.AsRef(in vec));
        Unsafe.Add(ref address, offset) = value;
    }
}
