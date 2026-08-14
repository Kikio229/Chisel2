using System;

namespace Chisel.Framework;

public interface ISampler
{
    float DetailBias { get; }
    SamplerFilterMode FilterMode { get; }
    SamplerWrapMode WrapMode { get; }
}
