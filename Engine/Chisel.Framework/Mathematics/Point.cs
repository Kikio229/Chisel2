using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct Point : IEquatable<Point>, IFormattable
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point()
        : this(0, 0)
    {
 
    }

    public Point(int val)
        : this(val, val)
    {

    }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
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
            X.ToString(format, formatProvider),
            Y.ToString(format, formatProvider));
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
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Point other)
    {
        return (X == other.X) && (Y == other.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Point)
        {
            return Equals((Point)obj);
        }

        return false;
    }
}