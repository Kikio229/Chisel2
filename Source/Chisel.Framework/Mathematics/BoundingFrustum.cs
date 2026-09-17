using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct BoundingFrustum : IEquatable<BoundingFrustum>, IFormattable
{
    public Plane[] Planes { get; private set; }
    public Vector3[] Corners { get; private set; }
    public const int PlaneCount = 6;
    public const int CornerCount = 8;

    public BoundingFrustum()
    {
        Planes = new Plane[PlaneCount];
        Corners = new Vector3[CornerCount];
    }

    public BoundingType ContainsPoint(Vector3 point)
    {
        for (int i = 0; i < PlaneCount; ++i)
        {
            // TODO: we might want to inline this for performance reasons
            if (point.X * Planes[i].Normal.X + point.Y * Planes[i].Normal.Y + point.Z * Planes[i].Normal.Z + Planes[i].Distance > 0)
            {
                return BoundingType.Disjoint;
            }
        }

        return BoundingType.Contains;
    }

    public BoundingType ContainsBox(BoundingBox box)
    {
        bool intersects = false;

        for (int i = 0; i < PlaneCount; i++)
        {
            PlaneIntersectType interType = box.IntersectsPlane(Planes[i]);

            switch (interType)
            {
                case PlaneIntersectType.Front:
                    return BoundingType.Disjoint;

                case PlaneIntersectType.Intersect:
                    intersects = true;
                    break;
            }
        }

        return intersects ? BoundingType.Intersects : BoundingType.Contains;
    }

    public BoundingType ContainsFrustum(BoundingFrustum frustum)
    {
        if (this == frustum)
        {
            return BoundingType.Contains;
        }

        bool intersects = false;

        for (int i = 0; i < PlaneCount; ++i)
        {
            PlaneIntersectType intersect = frustum.IntersectsPlane(Planes[i]);

            switch (intersect)
            {
                case PlaneIntersectType.Front:
                    return BoundingType.Disjoint;

                case PlaneIntersectType.Intersect:
                    intersects = true;
                    break;
            }
        }

        return intersects ? BoundingType.Intersects : BoundingType.Contains;
    }

    public BoundingType ContainsSphere(BoundingSphere sphere)
    {
        bool intersects = false;

        for (int i = 0; i < PlaneCount; ++i)
        {
            PlaneIntersectType intersect = sphere.IntersectsPlane(Planes[i]);

            switch (intersect)
            {
                case PlaneIntersectType.Front:
                    return BoundingType.Disjoint;

                case PlaneIntersectType.Intersect:
                    intersects = true;
                    break;
            }
        }

        return intersects ? BoundingType.Intersects : BoundingType.Contains;
    }

    public bool IntersectsBox(BoundingBox box)
    {
        BoundingType type = ContainsBox(box);
        return type != BoundingType.Disjoint;
    }

    public bool IntersectsFrustum(BoundingFrustum frustum)
    {
        return ContainsFrustum(frustum) != BoundingType.Disjoint;
    }

    public bool IntersectsSphere(BoundingSphere sphere)
    {
        BoundingType type = ContainsSphere(sphere);
        return type != BoundingType.Disjoint;
    }

    public PlaneIntersectType IntersectsPlane(Plane plane)
    {
        PlaneIntersectType type = plane.Intersects(Corners[0]);

        for (int i = 1; i < Corners.Length; i++)
        {
            if (plane.Intersects(Corners[i]) != type)    
            {
                type = PlaneIntersectType.Intersect;
            }
        }

        return type;
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(
            "({0}, {1})",
            Planes.ToString(),
            Corners.ToString());
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        return Planes.GetHashCode() ^ Corners.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(BoundingFrustum other)
    {
        return (Planes == other.Planes && Corners == other.Corners);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is BoundingFrustum)
        {
            return Equals((BoundingFrustum)obj);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(BoundingFrustum left, BoundingFrustum right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(BoundingFrustum left, BoundingFrustum right)
    {
        return !left.Equals(right);
    }
}
