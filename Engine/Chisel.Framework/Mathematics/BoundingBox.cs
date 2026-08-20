using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct BoundingBox : IEquatable<BoundingBox>, IFormattable
{
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public const int CornerCount = 8;

    public BoundingBox()
        : this(Vector3.Zero, Vector3.Zero)
    {

    }

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public BoundingType ContainsPoint(Vector3 point)
    {
        if (point.X < Min.X || point.X > Max.X || point.Y < Min.Y ||
            point.Y > Max.Y  || point.Z < Min.Z || point.Z > Max.Z)
        {
            return BoundingType.Disjoint;
        }
        else
        {
            return BoundingType.Contains;
        }
    }

    public BoundingType ContainsBox(BoundingBox box)
    {
        if (box.Max.X < Min.X || box.Min.X > Max.X || box.Max.Y < Min.Y || 
            box.Min.Y > Max.Y || box.Max.Z < Min.Z || box.Min.Z > Max.Z)
        {
            return BoundingType.Disjoint;
        }
            
        if (box.Min.X >= Min.X && box.Max.X <= Max.X && box.Min.Y >= Min.Y
            && box.Max.Y <= Max.Y && box.Min.Z >= Min.Z && box.Max.Z <= Max.Z)
        {
            return BoundingType.Contains;
        }
            
        return BoundingType.Intersects;
    }

    public BoundingType ContainsFrustum(BoundingFrustum frustum)
    {
        int i;
        BoundingType type;
        Vector3[] corners = frustum.Corners;

        for (i = 0; i < corners.Length; i++)
        {
            type = ContainsPoint(corners[i]);    

            if (type == BoundingType.Disjoint)
            {
                break;
            }
        }

        if (i == corners.Length)
        {
            return BoundingType.Contains;
        }
        if (i != 0)
        {
            return BoundingType.Intersects;
        }

        i++;

        for (; i < corners.Length; i++)
        {
            type = ContainsPoint(corners[i]);

            if (type != BoundingType.Contains)
            {
                return BoundingType.Intersects;
            }  
        }

        return BoundingType.Contains;
    }

    public BoundingType ContainsSphere(BoundingSphere sphere)
    {
        if (sphere.Center.X - Min.X >= sphere.Radius  && sphere.Center.Y - Min.Y >= sphere.Radius
            && sphere.Center.Z - Min.Z >= sphere.Radius && Max.X - sphere.Center.X >= sphere.Radius
            && Max.Y - sphere.Center.Y >= sphere.Radius && Max.Z - sphere.Center.Z >= sphere.Radius)
        {
            return BoundingType.Contains;
        }
            

        double dmin = 0;
        double e = sphere.Center.X - Min.X;

        if (e < 0)
        {
            if (e < -sphere.Radius)
            {
                return BoundingType.Disjoint;
            }

            dmin += e * e;
        }
        else
        {
            e = sphere.Center.X - Max.X;

            if (e > 0)
            {
                if (e > sphere.Radius)
                {
                    return BoundingType.Disjoint;
                }

                dmin += e * e;
            }
        }

        e = sphere.Center.Y - Min.Y;

        if (e < 0)
        {
            if (e < -sphere.Radius)
            {
                return BoundingType.Disjoint;
            }

            dmin += e * e;
        }
        else
        {
            e = sphere.Center.Y - Max.Y;

            if (e > 0)
            {
                if (e > sphere.Radius)
                {
                    return BoundingType.Disjoint;
                }

                dmin += e * e;
            }
        }

        e = sphere.Center.Z - Min.Z;

        if (e < 0)
        {
            if (e < -sphere.Radius)
            {
                return BoundingType.Disjoint;
            }

            dmin += e * e;
        }
        else
        {
            e = sphere.Center.Z - Max.Z;

            if (e > 0)
            {
                if (e > sphere.Radius)
                {
                    return BoundingType.Disjoint;
                }

                dmin += e * e;
            }
        }

        if (dmin <= sphere.Radius * sphere.Radius)
        {
            return BoundingType.Intersects;
        }
            
        return BoundingType.Disjoint;
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
        Vector3 posVert = Vector3.Zero;
        Vector3 negVert = Vector3.Zero;

        if (plane.Normal.X >= 0)
        {
            posVert.X = Max.X;
            negVert.X = Min.X;
        }
        else
        {
            posVert.X = Min.X;
            negVert.X = Max.X;
        }

        if (plane.Normal.Y >= 0)
        {
            posVert.Y = Max.Y;
            negVert.Y = Min.Y;
        }
        else
        {
            posVert.Y = Min.Y;
            negVert.Y = Max.Y;
        }

        if (plane.Normal.Z >= 0)
        {
            posVert.Z = Max.Z;
            negVert.Z = Min.Z;
        }
        else
        {
            posVert.Z = Min.Z;
            negVert.Z = Max.Z;
        }

        float distance = plane.Normal.X * negVert.X + plane.Normal.Y * negVert.Y + plane.Normal.Z * negVert.Z + plane.Distance;

        if (distance > 0)
        {
            return PlaneIntersectType.Front;
        }

        distance = plane.Normal.X * posVert.X + plane.Normal.Y * posVert.Y + plane.Normal.Z * posVert.Z + plane.Distance;

        if (distance < 0)
        {
            return PlaneIntersectType.Back;
        }

        return PlaneIntersectType.Intersect;
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
            Min.ToString(format, formatProvider),
            Max.ToString(format, formatProvider));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return ToString(null, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return Min.GetHashCode() ^ Max.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(BoundingBox other)
    {
        return (Min == other.Min && Max == other.Max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is BoundingBox)
        {
            return Equals((BoundingBox)obj);
        }

        return false;
    }
}
