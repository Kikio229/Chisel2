using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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

    private Vector256<float> _top, _bottom;

    public Matrix()
    {
        _top = Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
        _bottom = Vector256.Create(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
    }

    public Matrix(float val)
    {
        _top = Vector256.Create(val, val, val, val, val, val, val, val);
        _bottom = Vector256.Create(val, val, val, val, val, val, val, val);
    }

    // 2x2

    public Matrix(Vector2 vec1, Vector2 vec2)
    {
        _top = Vector256.Create(vec1.X, vec1.Y, 0f, 0f, vec2.X, vec2.Y, 0f, 0f);
        _bottom = Vector256.Create(0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
    }

    public Matrix(float m11, float m12, float m21, float m22)
    {
        _top = Vector256.Create(m11, m12, 0f, 0f, m21, m22, 0f, 0f);
        _bottom = Vector256.Create(0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
    }

    // 3x3

    public Matrix(Vector3 vec1, Vector3 vec2, Vector3 vec3)
    {
        _top = Vector256.Create(vec1.X, vec1.Y, vec1.Z, 0f, vec2.X, vec2.Y, vec2.Z, 0f);
        _bottom = Vector256.Create(vec3.X, vec3.Y, vec3.Z, 0f, 0f, 0f, 0f, 1f);
    }

    public Matrix(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
    {
        _top = Vector256.Create(m11, m12, m13, 0f, m21, m22, m23, 0f);
        _bottom = Vector256.Create(m31, m32, m33, 0f, 0f, 0f, 0f, 1f);
    }

    // 4x4

    public Matrix(Vector4 vec1, Vector4 vec2, Vector4 vec3, Vector4 vec4) 
    {
        _top = Vector256.Create(vec1.X, vec1.Y, vec1.Z, vec1.W, vec2.X, vec2.Y, vec2.Z, vec2.W);
        _bottom = Vector256.Create(vec3.X, vec3.Y, vec3.Z, vec3.W, vec4.X, vec4.Y, vec4.Z, vec4.W);
    }

    public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
    {
        _top = Vector256.Create(m11, m12, m13, m14, m21, m22, m23, m24);
        _bottom = Vector256.Create(m31, m32, m33, m34, m41, m42, m43, m44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Matrix(Vector256<float> top, Vector256<float> bottom)
    {
        _top = top;
        _bottom = bottom;
    }

    public Matrix Invert()
    {
        Matrix result = Matrix.Zero;

        float n1, n2, n3, n4, n5, n6, n7, n8, n9, n10;
        float n11, n12, n13, n14, n15, n16, n17, n18, n19, n20;
        float n21, n22, n23;

        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> r1, r2, r3, r4;

            r1 = _top.GetLower();
            r2 = _top.GetUpper();
            r3 = _bottom.GetLower();
            r4 = _bottom.GetUpper();

            Vector128<int> mask1, mask2, mask3, mask4;
            mask1 = Vector128.Create(2, 1, 1, 0);
            mask2 = Vector128.Create(0);
            mask3 = Vector128.Create(3, 3, 2, 3);
            mask4 = Vector128.Create(2, 1, 0, 0);

            // Shuffling operands

            Vector128<float> v1a, v1b, v2a, v2b, v3a, v3b;
            Vector128<float> v4a, v4b, v5a, v5b, v6a, v6b;

            v1a = Vector128.Shuffle(r3, mask1); // M33, M32, M32, M31
            v1b = Vector128.Shuffle(r3, mask2); // M31, M31, 0, 0
            v2a = Vector128.Shuffle(r3, mask3); // M34, M34, M33, M34
            v2b = Vector128.Shuffle(r3, mask4); // M33, M32, 0, 0

            v3a = Vector128.Shuffle(r4, mask1); // M43, M42, M42, M41
            v3b = Vector128.Shuffle(r4, mask2); // M41, M41, 0, 0
            v4a = Vector128.Shuffle(r4, mask3); // M44, M44, M43, M44
            v4b = Vector128.Shuffle(r4, mask4); // M43, M42, 0, 0

            v5a = Vector128.Shuffle(r2, mask1); // M23, M22, M22, M21
            v5b = Vector128.Shuffle(r2, mask2); // M21, M21, 0, 0
            v6a = Vector128.Shuffle(r2, mask3); // M24, M24, M23, M24
            v6b = Vector128.Shuffle(r2, mask4); // M23, M22, 0, 0

            // Final stuff before cofactor combining

            Vector128<float> f1, f2, f3, f4, f5, f6;

            f1 = Vector128.Subtract(Vector128.Multiply(v1a, v4a), Vector128.Multiply(v2a, v3a));
            f2 = Vector128.Subtract(Vector128.Multiply(v1b, v4b), Vector128.Multiply(v2b, v3b));
            f3 = Vector128.Subtract(Vector128.Multiply(v5a, v4a), Vector128.Multiply(v6a, v3a));
            f4 = Vector128.Subtract(Vector128.Multiply(v5b, v4b), Vector128.Multiply(v6b, v3b));
            f5 = Vector128.Subtract(Vector128.Multiply(v5a, v2a), Vector128.Multiply(v6a, v1a));
            f6 = Vector128.Subtract(Vector128.Multiply(v5b, v2b), Vector128.Multiply(v6b, v1b));

            // Final comboing part 1

            n1 = f1.GetElement(0);
            n2 = f1.GetElement(1);
            n3 = f1.GetElement(2);
            n4 = f1.GetElement(3);
            n5 = f2.GetElement(0);
            n6 = f2.GetElement(1);

            float m11, m12, m13, m14, m21, m22, m23, m24;

            m11 = r1.GetElement(0);
            m12 = r1.GetElement(1);
            m13 = r1.GetElement(2);
            m14 = r1.GetElement(3);
            m21 = r2.GetElement(0);
            m22 = r2.GetElement(1);
            m23 = r2.GetElement(2);
            m24 = r2.GetElement(3);

            n7 = (m22 * n1 - m23 * n2 + m24 * n3);
            n8 = -(m21 * n1 - m23 * n4 + m24 * n5);
            n9 = (m21 * n2 - m22 * n4 + m24 * n6);
            n10 = -(m21 * n3 - m22 * n5 + m23 * n6);
            n11 = 1.0f / (m11 * n7 + m12 * n8 + m13 * n9 + m14 * n10);

            n12 = f3.GetElement(0);
            n13 = f3.GetElement(1);
            n14 = f3.GetElement(2);
            n15 = f3.GetElement(3);
            n16 = f4.GetElement(0);
            n17 = f4.GetElement(1);
            n18 = f5.GetElement(0);
            n19 = f5.GetElement(1);
            n20 = f5.GetElement(2);
            n21 = f5.GetElement(3);
            n22 = f6.GetElement(0);
            n23 = f6.GetElement(1);

            // Final comboing part 2

            float o12, o22, o32, o42, o13, o23, o33, o43, o14, o24, o34, o44;

            o12 = -(m12 * n1 - m13 * n2 + m14 * n3);
            o22 = (m11 * n1 - m13 * n4 + m14 * n5);
            o32 = -(m11 * n2 - m12 * n4 + m14 * n6);
            o42 = (m11 * n3 - m12 * n5 + m13 * n6);

            o13 = (m12 * n12 - m13 * n13 + m14 * n14);
            o23 = -(m11 * n12 - m13 * n15 + m14 * n16);
            o33 = (m11 * n13 - m12 * n15 + m14 * n17);
            o43 = -(m11 * n14 - m12 * n16 + m13 * n17);

            o14 = -(m12 * n18 - m13 * n19 + m14 * n20);
            o24 = (m11 * n18 - m13 * n21 + m14 * n22);
            o34 = -(m11 * n19 - m12 * n21 + m14 * n23);
            o44 = (m11 * n20 - m12 * n22 + m13 * n23);

            // Rows and scale by (1 / det) with two 8-wide AVX mult
            Vector256<float> out12 = Vector256.Create(n7, o12, o13, o14, n8, o22, o23, o24);
            Vector256<float> out34 = Vector256.Create(n9, o32, o33, o34, n10, o42, o43, o44);
            Vector256<float> det = Vector256.Create(n11);

            result = new Matrix(Vector256.Multiply(out12, det), Vector256.Multiply(out34, det));
            return result;
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

    public Matrix Negate()
    {
        Matrix result = Matrix.Zero;

        if (Vector128.IsHardwareAccelerated)
        {
            Vector256<float> mask = Vector256.Create(-0f);
            result = new Matrix(Vector256.Xor(_top, mask), Vector256.Xor(_bottom, mask));
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

    public Matrix Transpose()
    {
        Matrix result = Matrix.Zero;

        // .NET is lame and doesn't include any clever AVX-style
        // vector instructions in its intrinsics library
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> r1, r2, r3, r4;
            r1 = _top.GetLower();
            r2 = _top.GetUpper();
            r3 = _bottom.GetLower();
            r4 = _bottom.GetUpper();

            Vector128<int> mask1, mask2;
            mask1 = Vector128.Create(0, 0, 1, 1);
            mask2 = Vector128.Create(0, 1, 2, 3);

            Vector128<float> t1, t2, t3, t4;
            t1 = Vector128.Shuffle(r1, mask1);
            t2 = Vector128.Shuffle(r2, mask1);
            t3 = Vector128.Shuffle(r3, mask1);
            t4 = Vector128.Shuffle(r4, mask1);

            Vector128<float> c1, c2, c3, c4;

            c1 = Vector128.Shuffle(Vector128.Create(
                r1.GetElement(0),
                r2.GetElement(0),
                r3.GetElement(0),
                r4.GetElement(0)),
                mask2);

            c2 = Vector128.Shuffle(Vector128.Create(
                r1.GetElement(1),
                r2.GetElement(1),
                r3.GetElement(1),
                r4.GetElement(1)),
                mask2);

            c3 = Vector128.Shuffle(Vector128.Create(
                r1.GetElement(2),
                r2.GetElement(2),
                r3.GetElement(2),
                r4.GetElement(2)),
                mask2);

            c4 = Vector128.Shuffle(Vector128.Create(
                r1.GetElement(3),
                r2.GetElement(3),
                r3.GetElement(3),
                r4.GetElement(3)),
                mask2);

            return new Matrix(Vector256.Create(c1, c2), Vector256.Create(c3, c4));
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

    public Matrix InterpAsTwoByTwo()
    {
        return new Matrix();
    }

    public Matrix InterpAsThreeByThree()
    {
        return new Matrix();
    }

    public System.Numerics.Matrix4x4 ToNumerics()
    {
        return new System.Numerics.Matrix4x4(
            M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44);
    }

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

    public static Matrix FromYawPitchRoll(float yaw, float pitch, float roll)
    {
        return FromQuaternion(Quaternion.FromYawPitchRoll(yaw, pitch, roll));
    }

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

    public static Matrix FromScale(Vector3 scale)
    {
        Matrix result = Matrix.Zero;
        result.M11 = scale.X;
        result.M22 = scale.Y;
        result.M33 = scale.Z;
        result.M44 = 1f;
        return result;
    }

    public static Matrix FromLookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        Matrix result = Matrix.Zero;

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

    public static Matrix FromOrthographic(float left, float right, float bottom, float top, float near, float far)
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

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(M11);
        hash.Add(M12);
        hash.Add(M13);
        hash.Add(M14);
        hash.Add(M21);
        hash.Add(M22);
        hash.Add(M23);
        hash.Add(M24);
        hash.Add(M31);
        hash.Add(M32);
        hash.Add(M33);
        hash.Add(M34);
        hash.Add(M41);
        hash.Add(M42);
        hash.Add(M43);
        hash.Add(M44);
        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Matrix other)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return Vector256.EqualsAll(_top, other._top) && Vector256.EqualsAll(_bottom, other._bottom);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Add(Matrix mat, float val)
    {
        return Add(mat, new Matrix(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Add(Matrix left, Matrix right)
    {
        if (Vector128.IsHardwareAccelerated)
        {
            return new Matrix(Vector256.Add(left._top, right._top), Vector256.Add(left._bottom, right._bottom));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix mat, float val)
    {
        return Multiply(mat, new Matrix(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix left, Matrix right)
    {
        Matrix result = Matrix.Zero;

        // Like Quaternions, Matrix multiplication is funky
        if (Vector256.IsHardwareAccelerated)
        {
            Vector256<int> mask1, mask2, mask3, mask4, mask5, mask6;
            mask1 = Vector256.Create(0, 1, 2, 3, 0, 1, 2, 3);
            mask2 = Vector256.Create(4, 5, 6, 7, 4, 5, 6, 7);
            mask3 = Vector256.Create(0, 0, 0, 0, 4, 4, 4, 4);
            mask4 = Vector256.Create(1, 1, 1, 1, 5, 5, 5, 5);
            mask5 = Vector256.Create(2, 2, 2, 2, 6, 6, 6, 6);
            mask6 = Vector256.Create(3, 3, 3, 3, 7, 7, 7, 7);

            Vector256<float> r1, r2, r3, r4;
            r1 = Vector256.Shuffle(right._top, mask1);
            r2 = Vector256.Shuffle(right._top, mask2);
            r3 = Vector256.Shuffle(right._bottom, mask1);
            r4 = Vector256.Shuffle(right._bottom, mask2);

            Vector256<float> top = Vector256.Shuffle(left._top, mask3) * r1;
            top += Vector256.Shuffle(left._top, mask4) * r2;
            top += Vector256.Shuffle(left._top, mask5) * r3;
            top += Vector256.Shuffle(left._top, mask6) * r4;

            Vector256<float> bottom = Vector256.Shuffle(left._bottom, mask3) * r1;
            bottom += Vector256.Shuffle(left._bottom, mask4) * r2;
            bottom += Vector256.Shuffle(left._bottom, mask5) * r3;
            bottom += Vector256.Shuffle(left._bottom, mask6) * r4;

            result = new Matrix(top, bottom);
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
