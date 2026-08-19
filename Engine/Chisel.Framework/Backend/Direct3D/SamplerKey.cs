using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Backend.Direct3D;
internal readonly struct SamplerKey : IEquatable<SamplerKey>
{
    public readonly SamplerFilterMode FilterMode;
    public readonly SamplerWrapMode WrapMode;
    public readonly float DetailBias;

    public SamplerKey(SamplerFilterMode filterMode, SamplerWrapMode wrapMode, float detailBias)
    {
        FilterMode = filterMode;
        WrapMode = wrapMode;
        DetailBias = detailBias;
    }

    public bool Equals(SamplerKey other) => FilterMode == other.FilterMode && WrapMode == other.WrapMode && DetailBias.Equals(other.DetailBias);
    public override bool Equals(object obj) => obj is SamplerKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(FilterMode, WrapMode, DetailBias);
}