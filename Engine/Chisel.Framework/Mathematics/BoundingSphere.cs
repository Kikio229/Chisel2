using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct BoundingSphere : IEquatable<BoundingSphere>, IFormattable
{
    public float Radius { get; private set; }
    public Vector3 Center { get; private set; }

    public BoundingSphere() 
    {
        Radius = 0f;
        Center = Vector3.Zero;
    }

    public BoundingSphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public BoundingType ContainsPoint(Vector3 point)
    {
        float radSqr = Radius * Radius;
        float distSqr = Center.DistanceSquared(point);

        if (distSqr > radSqr)
        {
            return BoundingType.Disjoint;
        }
        else if (distSqr < radSqr)
        {
            return BoundingType.Contains;
        }
        else
        {
            return BoundingType.Intersects;
        }
    }

    public BoundingType ContainsBox(BoundingBox box)
    {
        //check if all corner is in sphere
        bool inside = true;

        Vector3[] corners = new Vector3[8]
        {
            new Vector3(box.Min.X, box.Max.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Min.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Min.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Min.Z),
            new Vector3(box.Min.X, box.Min.Y, box.Min.Z)
        };

        foreach (Vector3 corner in corners)
        {
            if (ContainsPoint(corner) == BoundingType.Disjoint)
            {
                inside = false;
                break;
            }
        }

        if (inside)
        {
            return BoundingType.Contains;
        }

        double dmin = 0;

        if (Center.X < box.Min.X)
        {
            dmin += (Center.X - box.Min.X) * (Center.X - box.Min.X);
        }
        else if (Center.X > box.Max.X)
        {
            dmin += (Center.X - box.Max.X) * (Center.X - box.Max.X);
        }

        if (Center.Y < box.Min.Y)
        {
            dmin += (Center.Y - box.Min.Y) * (Center.Y - box.Min.Y);
        }
        else if (Center.Y > box.Max.Y)
        {
            dmin += (Center.Y - box.Max.Y) * (Center.Y - box.Max.Y);
        }

        if (Center.Z < box.Min.Z)
        {
            dmin += (Center.Z - box.Min.Z) * (Center.Z - box.Min.Z);
        }
        else if (Center.Z > box.Max.Z)
        {
            dmin += (Center.Z - box.Max.Z) * (Center.Z - box.Max.Z);
        }
            
        if (dmin <= Radius * Radius)
        {
            return BoundingType.Intersects;
        }
            
        return BoundingType.Disjoint;
    }

    public BoundingType ContainsFrustum(BoundingFrustum frustum)
    {
        bool inside = true;
        Vector3[] corners = frustum.Corners;

        foreach (Vector3 corner in corners)
        {
            if (ContainsPoint(corner) == BoundingType.Disjoint)
            {
                inside = false;
                break;
            }
        }
        if (inside)
        {
            return BoundingType.Contains;
        }

        if (Radius * Radius >= 0)
        {
            return BoundingType.Intersects;
        }

        return BoundingType.Disjoint;
    }

    public BoundingType ContainsSphere(BoundingSphere sphere)
    {
        float distSqr = Center.DistanceSquared(sphere.Center);

        if (distSqr > (sphere.Radius + Radius) * (sphere.Radius + Radius))
        {
            return BoundingType.Disjoint;
        }
        else if (distSqr <= (Radius - sphere.Radius) * (Radius - sphere.Radius))
        {
            return BoundingType.Contains;
        }
        else
        {
            return BoundingType.Intersects;
        }
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
        float distance = Center.DotProduct(plane.Normal);
        distance += plane.Distance;

        if (distance > Radius)
        {
            return PlaneIntersectType.Front;
        }    
        else if (distance < -Radius)
        {
            return PlaneIntersectType.Back;
        }   
        else
        {
            return PlaneIntersectType.Intersect;
        }
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
            Center.ToString(format, formatProvider),
            Radius.ToString(format, formatProvider));
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        Hasher hasher = new Hasher();
        hasher.Add(Radius);
        return (int)hasher.Finalize32() ^ Center.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(BoundingSphere other)
    {
        return (Radius == other.Radius) && (Center == other.Center);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is BoundingSphere)
        {
            return Equals((BoundingSphere)obj);
        }

        return false;
    }
}
