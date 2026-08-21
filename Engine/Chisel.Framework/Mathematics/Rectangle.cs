using System;
using System.Runtime.CompilerServices;

namespace Chisel.Framework;

public struct Rectangle : IEquatable<Rectangle>, IFormattable
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int Left
    {
        get => X;
    }

    public int Right
    {
        get => (X + Width);
    }

    public int Top
    {
        get => Y;
    }

    public int Bottom
    {
        get => (Y + Height);
    }

    public Point Size
    {
        get => new Point(Width, Height);
    }

    public Point Center
    {
        get => new Point(X + (Width / 2), Y + (Height / 2));
    }

    public static Rectangle Empty = new Rectangle(0, 0, 0, 0);

    public Rectangle()
    {
        X = 0;
        Y = 0;
        Width = 0;
        Height = 0;
    }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rectangle Intersect(Rectangle rect)
    {
        if (Intersects(rect))
        {
            int right = (X + Width).Min(rect.X + rect.Width);
            int left = X.Max(rect.X);
            int top = Y.Max(rect.Y);
            int bottom = (Y + Height).Min(rect.Y + rect.Height);
            return new Rectangle(left, top, right - left, bottom - top);
        }

        return Rectangle.Empty;
    }

    public readonly bool Intersects(Rectangle value)
    {
        return value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;
    }

    public readonly bool ContainsValues(int x, int y)
    {
        return (X <= x) && (x < (X + Width)) && (Y <= y) && (y < (Y + Height));
    }

    public readonly bool ContainsValues(float x, float y)
    {
        return (X <= x) && (x < (X + Width)) && (Y <= y) && (y < (Y + Height));
    }

    public readonly bool ContainsPoint(Point pnt)
    {
        return (X <= pnt.X) && (pnt.X < (X + Width)) && (Y <= pnt.Y) && (pnt.Y < (Y + Height));
    }
    public readonly bool ContainsVector(Vector2 vec)
    {
        return (X <= vec.X) && (vec.X < (X + Width)) && (Y <= vec.Y) && (vec.Y < (Y + Height));
    }

    public readonly bool ContainsRectangle(Rectangle rect)
    {
        return (X <= rect.X) && (rect.X < (X + Width)) && (Y <= rect.Y) && (rect.Y < (Y + Height));
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
            Width.ToString(format, formatProvider),
            Height.ToString(format, formatProvider));
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
        hasher.Add(Width);
        hasher.Add(Height);
        return (int)hasher.Finalize32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Rectangle other)
    {
        return (X == other.X) && (Y == other.Y) && (Width == other.Width) && (Height == other.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (obj != null && obj is Rectangle)
        {
            return Equals((Rectangle)obj);
        }

        return false;
    }

    public static bool operator ==(Rectangle left, Rectangle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rectangle left, Rectangle right)
    {
        return !left.Equals(right);
    }
}