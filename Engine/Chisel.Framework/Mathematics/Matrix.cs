using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public struct Matrix : IEquatable<Matrix>, IFormattable
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
     
    public static Matrix Zero => new Matrix(0f);
    public static Matrix One => new Matrix(1f);
    public static Matrix Identity = new Matrix(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

    private Vector256<float> _top;
    private Vector256<float> _bottom;

    public Matrix()
        : this(Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f), 
               Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f))
    {

    }

    public Matrix(float val)
        : this(Vector256.Create(val, val, val, val, val, val, val, val),
               Vector256.Create(val, val, val, val, val, val, val, val))
    {

    }

    public Matrix(Vector4 vec0, Vector4 vec1, Vector4 vec2, Vector4 vec3) 
        : this(Vector256.Create(vec0.X, vec0.Y, vec0.Z, vec0.W, vec1.X, vec1.Y, vec1.Z, vec1.W), 
               Vector256.Create(vec2.X, vec2.Y, vec2.Z, vec2.W, vec3.X, vec3.Y, vec3.Z, vec3.W))
    {

    }

    public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
        : this(Vector256.Create(m11, m12, m13, m14, m21, m22, m23, m24),
               Vector256.Create(m31, m32, m33, m34, m41, m42, m43, m44))
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Matrix(Vector256<float> top, Vector256<float> bottom)
    {
        _top = top;
        _bottom = bottom;
    }

    /// <summary>
    /// Returns a matrix with that's been inverted
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Invert()
    {
        Matrix result = Matrix.Zero;

        float n1, n2, n3, n4, n5, n6, n7, n8, n9, n10;
        float n11, n12, n13, n14, n15, n16, n17, n18, n19, n20;
        float n21, n22, n23;

        // TODO: Some SIMD optimizations if possible?

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

    /// <summary>
    /// Returns a matrix all of the values flipped or negated
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Negate()
    {
        if (Avx.IsSupported)
        {
            Vector256<float> mask = Vector256.Create(-0f);
            return new Matrix(Avx.Xor(_top, mask), Avx.Xor(_bottom, mask));
        }

        Matrix result = Matrix.Zero;

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

    /// <summary>
    /// Returns a matrix with swapped rows and columns
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Transpose()
    {
        // We're shuffiling around individual rows, so we're using SSE instead of AVX for once
        if (Sse.IsSupported)
        {
            Vector128<float> row1, row2, row3, row4, tmp1, tmp2, tmp3, tmp4, col1, col2, col3, col4;

            row1 = Vector128.Create(M11, M12, M13, M14);
            row2 = Vector128.Create(M21, M22, M23, M24);
            row3 = Vector128.Create(M31, M32, M33, M34);
            row4 = Vector128.Create(M41, M42, M43, M44);

            tmp1 = Sse.UnpackLow(row1, row2);
            tmp2 = Sse.UnpackHigh(row1, row2);
            tmp3 = Sse.UnpackLow(row3, row4);
            tmp4 = Sse.UnpackHigh(row3, row4);

            col1 = Sse.MoveLowToHigh(tmp1, tmp3);
            col2 = Sse.MoveHighToLow(tmp3, tmp1);
            col3 = Sse.MoveLowToHigh(tmp2, tmp4);
            col4 = Sse.MoveHighToLow(tmp4, tmp2);

            return new Matrix(Vector256.Create(col1, col2), Vector256.Create(col3, col4));
        }

        Matrix result = Matrix.Zero;

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

    /// <summary>
    /// Returns a standard .NET numerics matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Numerics.Matrix4x4 ToNumerics()
    {
        return new System.Numerics.Matrix4x4(
            M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44);
    }

    /// <summary>
    /// Returns a rotation matrix around the X axis
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromRotationX(float radians)
    {
        Matrix result = Matrix.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M22 = val1;
        result.M23 = val2;
        result.M32 = -val2;
        result.M33 = val1;

        return result;
    }

    /// <summary>
    /// Returns a rotation matrix around the Y axis
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromRotationY(float radians)
    {
        Matrix result = Matrix.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M11 = val1;
        result.M13 = -val2;
        result.M31 = val2;
        result.M33 = val1;

        return result;
    }

    /// <summary>
    /// Returns a rotation matrix around the Z axis
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromRotationZ(float radians)
    {
        Matrix result = Matrix.Identity;

        float val1 = radians.Cos();
        float val2 = radians.Sin();

        result.M11 = val1;
        result.M12 = val2;
        result.M21 = -val2;
        result.M22 = val1;

        return result;
    }

    /// <summary>
    /// Returns a matrix which contains the rotation around specified axis
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromAxisAngle(Vector3 axis, float angle)
    {
        Matrix result = Matrix.Zero;
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

    /// <summary>
    /// Returns a rotation matrix from a quaternion
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromQuaternion(Quaternion quaternion)
    {
        Matrix result = Matrix.Zero;
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

    /// <summary>
    /// Returns a rotation matrix from yaw, pitch, and roll values
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromYawPitchRoll(float yaw, float pitch, float roll)
    {
        return FromQuaternion(Quaternion.FromYawPitchRoll(yaw, pitch, roll));
    }

    /// <summary>
    /// Returns a translation matrix from a 3D position vector
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromTranslation(Vector3 translate)
    {
        Matrix result = Matrix.Zero;

        result.M11 = 1f;
        result.M22 = 1f;
        result.M33 = 1f;
        result.M41 = translate.X;
        result.M42 = translate.Y;
        result.M43 = translate.Z;
        result.M44 = 1f;

        return result;
    }

    /// <summary>
    /// Returns a scale matrix from a 3D scale vector
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromScale(Vector3 scale)
    {
        Matrix result = Matrix.Zero;

        result.M11 = scale.X;
        result.M22 = scale.Y;
        result.M33 = scale.Z;
        result.M44 = 1f;

        return result;
    }

    /// <summary>
    /// Returns a view matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromLookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Matrix result = Matrix.Zero;

        Vector3 vectorA = (position - target).Normalize();
        Vector3 vectorB = up.CrossProduct(vectorA).Normalize();
        Vector3 vectorC = vectorA.CrossProduct(vectorB);

        result.M11 = vectorB.X;
        result.M12 = vectorC.X;
        result.M13 = vectorA.X;

        result.M21 = vectorB.Y;
        result.M22 = vectorC.Y;
        result.M23 = vectorA.Y;

        result.M31 = vectorB.Z;
        result.M32 = vectorC.Z;
        result.M33 = vectorA.Z;

        result.M41 = -vectorB.DotProduct(position);
        result.M42 = -vectorC.DotProduct(position);
        result.M43 = -vectorA.DotProduct(position);

        result.M44 = 1f;

        return result;
    }

    /// <summary>
    /// Returns an orthographic projection matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromOrthographic(float left, float right, float top, float bottom, float near, float far)
    {
        Matrix result = Matrix.Zero;

        result.M11 = (float)(2.0 / (right - left));
        result.M22 = (float)(2.0 / (top - bottom));
        result.M33 = (float)(1.0 / (near - far));
        result.M41 = (float)((left + right) / (left - right));
        result.M42 = (float)((top + bottom) / (bottom - top));
        result.M43 = (float)(near / (near - far));
        result.M44 = 1.0f;

        return result;
    }

    /// <summary>
    /// Returns an perspective projection matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromPerspective(float left, float right, float top, float bottom, float near, float far)
    {
        Matrix result = Matrix.Zero;

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

    /// <summary>
    /// Returns an perspective projection matrix using an aspect ratio and field-of-view
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromPerspectiveFov(float fovy, float ratio, float near, float far)
    {
        Matrix result = Matrix.Zero;

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
    public readonly bool Equals(Matrix other)
    {
        if (Avx.IsSupported)
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
        if (obj != null && obj is Matrix)
        {
            return Equals((Matrix)obj);
        }

        return false;
    }

    /// <summary>
    /// Adds a value directly to a matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Add(Matrix mat, float val)
    {
        return Add(mat, new Matrix(val));
    }

    /// <summary>
    /// Adds two matrices together
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Add(Matrix left, Matrix right)
    {
        if (Avx.IsSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Add(left._top, right._top);
            bottom = Avx.Add(left._bottom, right._bottom);
            return new Matrix(top, bottom);
        }

        return new Matrix(left.M11 + right.M11,
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

    /// <summary>
    /// Subtracts a value directly from a matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Subtract(Matrix mat, float val)
    {
        return Subtract(mat, new Matrix(val));
    }

    /// <summary>
    /// Subtracts two matrices from each other
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Subtract(Matrix left, Matrix right)
    {
        if (Avx.IsSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Subtract(left._top, right._top);
            bottom = Avx.Subtract(left._bottom, right._bottom);
            return new Matrix(top, bottom);
        }

        return new Matrix(left.M11 - right.M11,
            left.M12 - right.M12,
            left.M13 - right.M13,
            left.M14 - right.M14,
            left.M21 - right.M21,
            left.M22 - right.M22,
            left.M23 - right.M23,
            left.M24 - right.M24,
            left.M31 - right.M31,
            left.M32 - right.M32,
            left.M33 - right.M33,
            left.M34 - right.M34,
            left.M41 - right.M41,
            left.M42 - right.M42,
            left.M43 - right.M43,
            left.M44 - right.M44);
    }

    /// <summary>
    /// Multiplies a value directly to a matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix mat, float val)
    {
        return Multiply(mat, new Matrix(val));
    }

    /// <summary>
    /// Multiplies two matrices together
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix left, Matrix right)
    {
        if (Avx.IsSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Multiply(left._top, right._top);
            bottom = Avx.Multiply(left._bottom, right._bottom);
            return new Matrix(top, bottom);
        }

        return new Matrix(left.M11 * right.M11,
            left.M12 * right.M12,
            left.M13 * right.M13,
            left.M14 * right.M14,
            left.M21 * right.M21,
            left.M22 * right.M22,
            left.M23 * right.M23,
            left.M24 * right.M24,
            left.M31 * right.M31,
            left.M32 * right.M32,
            left.M33 * right.M33,
            left.M34 * right.M34,
            left.M41 * right.M41,
            left.M42 * right.M42,
            left.M43 * right.M43,
            left.M44 * right.M44);
    }

    /// <summary>
    /// Divides a value directly from a matrix
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Divide(Matrix mat, float val)
    {
        return Divide(mat, new Matrix(val));
    }

    /// <summary>
    /// Divides two matrices from each other
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Divide(Matrix left, Matrix right)
    {
        if (Avx.IsSupported)
        {
            Vector256<float> top, bottom;
            top = Avx.Divide(left._top, right._top);
            bottom = Avx.Divide(left._bottom, right._bottom);
            return new Matrix(top, bottom);
        }

        return new Matrix(left.M11 / right.M11,
            left.M12 / right.M12,
            left.M13 / right.M13,
            left.M14 / right.M14,
            left.M21 / right.M21,
            left.M22 / right.M22,
            left.M23 / right.M23,
            left.M24 / right.M24,
            left.M31 / right.M31,
            left.M32 / right.M32,
            left.M33 / right.M33,
            left.M34 / right.M34,
            left.M41 / right.M41,
            left.M42 / right.M42,
            left.M43 / right.M43,
            left.M44 / right.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(Matrix mat, float val)
    {
        return Add(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(Matrix left, Matrix right)
    {
        return Add(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(Matrix mat, float val)
    {
        return Subtract(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(Matrix left, Matrix right)
    {
        return Subtract(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(Matrix mat, float val)
    {
        return Multiply(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(Matrix left, Matrix right)
    {
        return Multiply(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix mat, float val)
    {
        return Divide(mat, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(Matrix left, Matrix right)
    {
        return Divide(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Matrix left, Matrix right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Matrix left, Matrix right)
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
