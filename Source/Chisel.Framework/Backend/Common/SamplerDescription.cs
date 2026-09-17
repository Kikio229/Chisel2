using System;

namespace Chisel.Framework;

public struct SamplerDescription
{
    public float DetailBias;
    public SamplerFilterMode FilterMode;
    public SamplerWrapMode WrapMode;

    public SamplerDescription()
    {
        FilterMode = SamplerFilterMode.Nearest;
        WrapMode = SamplerWrapMode.Repeat;
    }
}