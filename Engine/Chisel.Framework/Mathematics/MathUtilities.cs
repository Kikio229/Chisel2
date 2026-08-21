using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Chisel.Framework;

public static class MathUtilities
{
    public const double Pi = 3.14159265358979323846;
    public const double Tau = Pi * 2.0;
    public const double PiOverTwo = Pi / 2.0;
    public const double PiOverFour = Pi / 4.0;
    public const double Deg2Rad = Pi / 180.0;
    public const double Rad2Deg = 180.0 / Pi;
    public const double Epsilon = 1e-6; // Any smaller is probably overkill
    public const double Euler = 2.71828175; // Otherwise known as the constant E
    public const double Log10Euler = 0.4342945; // Base-10 logarithm with the power of E
    public const double Log2Euler = 1.442695; // Base-2 logarithm with the power of E

    // Float versions
    public const float PiF = 3.14159265358979323846f;
    public const float TauF = PiF * 2.0f;
    public const float PiOverTwoF = PiF / 2.0f;
    public const float PiOverFourF = PiF / 2.0f;
    public const float Deg2RadF = PiF / 180.0f;
    public const float Rad2DegF = 180.0f / PiF;
    public const float EpsilonF = 1e-6f; // Same deal as before
    public const float EulerF = 2.71828175f; // Same deal as before
    public const float Log10EulerF = 0.4342945f; // Same deal as before
    public const float Log2EulerF = 1.442695f; // Same deal as before

    // AVX requires a x86 CPU from at least 2011...
    // So as long as you're not using like a 1st gen i7 you this should be supported
    internal static bool X86SimdSupported = Avx.IsSupported; 
    internal static bool ArmSimdSupported = false; // Maybe...

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(this float value) => MathF.Abs(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Abs(this double value) => Math.Abs(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Acos(this float value) => MathF.Acos(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Acos(this double value) => Math.Acos(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Asin(this float value) => MathF.Asin(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Asin(this double value) => Math.Asin(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan(this float value) => MathF.Atan(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Atan(this double value) => Math.Atan(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan2(this float value, float other) => MathF.Atan2(value, other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Atan2(this double value, double other) => Math.Atan2(value, other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cbrt(this float value) => MathF.Cbrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Cbrt(this double value) => Math.Cbrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Ceiling(this float value) => MathF.Ceiling(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Ceiling(this double value) => Math.Ceiling(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(this float value) => MathF.Cos(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Cos(this double value) => Math.Cos(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Exp(this float value) => MathF.Exp(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Exp(this double value) => Math.Exp(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Floor(this float value) => MathF.Floor(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Floor(this double value) => Math.Floor(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(this float value) => MathF.Log(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Log(this double value) => Math.Log(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log2(this float value) => MathF.Log2(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Log2(this double value) => Math.Log2(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log10(this float value) => MathF.Log10(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Log10(this double value) => Math.Log10(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Pow(this float value, float power) => MathF.Pow(value, power);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Pow(this double value, double power) => Math.Pow(value, power);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(this float value) => MathF.Sin(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sin(this double value) => Math.Sin(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(this float value) => MathF.Sqrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sqrt(this double value) => Math.Sqrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tan(this float value) => MathF.Tan(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Tan(this double value) => Math.Tan(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int value, int min, int max)
    {
        value = (value > max) ? max : value;
        value = (value < min) ? min : value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(this float value, float min, float max)
    {
        value = (value > max) ? max : value;
        value = (value < min) ? min : value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp(this double value, double min, double max)
    {
        value = (value > max) ? max : value;
        value = (value < min) ? min : value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(this float value, float other)
    {
        return MathF.Abs(value - other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Distance(this double value, double other)
    {
        return Math.Abs(value - other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(this float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Lerp(this double from, double to, double amount)
    {
        return from + (to - from) * amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Min(this int value, int other)
    {
        return value < other ? value : other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(this float value, float other)
    {
        return value < other ? value : other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Min(this double value, double other)
    {
        return value < other ? value : other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Max(this int value, int other)
    {
        return value > other ? value : other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(this float value, float other)
    {
        return value > other ? value : other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Max(this double value, double other)
    {
        return value > other ? value : other;
    }
}
