using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct Plane : IEquatable<Plane>, IFormattable
{
    public float Distance { get; private set; }
    public Vector3 Normal { get; private set; }

    public Plane()  
    {
        Distance = 0f; 
        Normal = Vector3.Zero;
    }

    public Plane(float distance, Vector3 normal) 
    {
        Distance = distance;
        Normal = normal;
    }

    public Plane(float x, float y, float z, float w)
    {
        Distance = w;
        Normal = new Vector3(x, y, z);
    }

    public Plane(Vector4 vec)
    {
        Distance = vec.W;
        Normal = vec.XYZ;
    }

    public Plane(Vector3 vec1, Vector3 vec2, Vector3 vec3)
    {
        Vector3 vab = vec2 - vec1;
        Vector3 vac = vec3 - vec1;
        Vector3 cross = vab.CrossProduct(vac);
        Normal = cross.Normalize();
        Distance = -Normal.DotProduct(vec1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Dot(Vector4 value)
    {
        return Normal.DotProduct(value.XYZ) + (Distance * value.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DotNormal(Vector3 value)
    {
        return Normal.DotProduct(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DotCoordinate(Vector3 value)
    {
        return Normal.DotProduct(value) + Distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PlaneIntersectType Intersects(Vector3 point)
    {
        float distance = DotCoordinate(point);

        if (distance > 0)
        {
            return PlaneIntersectType.Front;
        }

        if (distance < 0)
        {
            return PlaneIntersectType.Back;
        }

        return PlaneIntersectType.Intersect;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Plane TransformByQuaternion(Quaternion quat)
    {
        return new Plane(Distance, Normal.TransformByQuaternion(quat));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Plane TransformByMatrix(Matrix4 matrix)
    {
        Matrix4 transMat = matrix.Invert().Transpose();
        Vector4 vector = new Vector4(Normal.X, Normal.Y, Normal.Z, Distance);
        Vector4 transVec = vector.TransformByMatrix(transMat);
        return new Plane(transVec);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Plane Normalize()
    {
        float length = Normal.Length();
        float factor = 1.0f / length;
        float distance = Distance * factor;
        Vector3 normal = Normal * factor;
        return new Plane(distance, normal);
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
            Normal.ToString(format, formatProvider),
            Distance.ToString(format, formatProvider));
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
        hasher.Add(Distance);
        return (int)hasher.Finalize32() ^ Normal.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Plane other)
    {
        return (Distance == other.Distance) && (Normal == other.Normal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Plane)
        {
            return Equals((Plane)obj);
        }

        return false;
    }
};