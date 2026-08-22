using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct Ray : IEquatable<Ray>, IFormattable
{
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }

    public Ray()
    {
        Position = Vector3.Zero;
        Direction = Vector3.Zero;
    }

    public Ray(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction;
    }

    public float? IntersectsBox(BoundingBox box)
    {
        float? min = null;
        float? max = null;

        if (Direction.X.Abs() < MathUtilities.EpsilonF)
        {
            if (Position.X < box.Min.X || Position.X > box.Max.X)
            {
                return null;
            }    
        }
        else
        {
            min = (box.Min.X - Position.X) / Direction.X;
            max = (box.Max.X - Position.X) / Direction.X;

            if (min > max)
            {
                float? temp = min;
                min = max;
                max = temp;
            }
        }

        if (Direction.Y.Abs() < MathUtilities.EpsilonF)
        {
            if (Position.Y < box.Min.Y || Position.Y > box.Max.Y)
            {
                return null;
            }
        }
        else
        {
            float minY = (box.Min.Y - Position.Y) / Direction.Y;
            float maxY = (box.Max.Y - Position.Y) / Direction.Y;

            if (minY > maxY)
            {
                float temp = minY;
                minY = maxY;
                maxY = temp;
            }

            if ((min.HasValue && min > maxY) || (max.HasValue && minY > max))
            {
                return null;
            }
               

            if (!min.HasValue || minY > min) min = minY;
            if (!max.HasValue || maxY < max) max = maxY;
        }

        if (Direction.Z.Abs() < MathUtilities.EpsilonF)
        {
            if (Position.Z < box.Min.Z || Position.Z > box.Max.Z)
            {
                return null;
            }
        }
        else
        {
            float minZ = (box.Min.Z - Position.Z) / Direction.Z;
            float maxZ = (box.Max.Z - Position.Z) / Direction.Z;

            if (minZ > maxZ)
            {
                float temp = minZ;
                minZ = maxZ;
                maxZ = temp;
            }

            if ((min.HasValue && min > maxZ) || (max.HasValue && minZ > max))
            {
                return null;
            }

            if (!min.HasValue || minZ > min) 
            {
                min = minZ;
            }

            if (!max.HasValue || maxZ < max) 
            {
                max = maxZ;
            }
        }

        if ((min.HasValue && min < 0) && max > 0)
        {
            return 0;
        }

        if (min < 0)
        {
            return null;
        }

        return min;
    }

    public float? IntersectsSphere(BoundingSphere sphere)
    {
        Vector3 diff = sphere.Center - Position;
        float diffSqr = diff.LengthSquared();
        float radSqr = sphere.Radius * sphere.Radius;;

        if (diffSqr < radSqr)
        {
            return 0.0f;
        }

        float distAlong = Direction.DotProduct(diff);

        if (distAlong < 0)
        {
            return null;
        }

        float dist = radSqr + distAlong * distAlong - diffSqr;
        return (dist < 0) ? null : distAlong - (float?)MathF.Sqrt(dist);
    }

    public float? IntersectsPlane(Plane plane)
    {
        var den = Direction.DotProduct(plane.Normal);

        if (den.Abs() < MathUtilities.EpsilonF)
        {
            return null;
        }

        float result = (-plane.Distance - plane.Normal.DotProduct(Position)) / den;

        if (result < 0.0f)
        {
            if (result < -0.00001f)
            {
                return null;
            }

            result = 0.0f;
        }

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
            "({0}, {1})",
            Position.ToString(format, formatProvider),
            Direction.ToString(format, formatProvider));
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public override readonly int GetHashCode()
    {
        return Position.GetHashCode() ^ Direction.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Ray other)
    {
        return Position.Equals(other.Position) && Direction.Equals(other.Direction);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Ray)
        {
            return Equals((Ray)obj);
        }

        return false;
    }

    public static bool operator ==(Ray left, Ray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Ray left, Ray right)
    {
        return !left.Equals(right);
    }
}
