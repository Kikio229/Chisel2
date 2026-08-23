using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

internal readonly struct D3DSamplerKey : IEquatable<D3DSamplerKey>
{
    public readonly float DetailBias;
    public readonly SamplerFilterMode FilterMode;
    public readonly SamplerWrapMode WrapMode;


    public D3DSamplerKey(SamplerFilterMode filterMode, SamplerWrapMode wrapMode, float detailBias)
    {
        FilterMode = filterMode;
        WrapMode = wrapMode;
        DetailBias = detailBias;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(D3DSamplerKey other)
    {
        return FilterMode == other.FilterMode && WrapMode == other.WrapMode && DetailBias.Equals(other.DetailBias);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj)
    {
        return obj is D3DSamplerKey other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return HashCode.Combine(FilterMode, WrapMode, DetailBias);
    }
}