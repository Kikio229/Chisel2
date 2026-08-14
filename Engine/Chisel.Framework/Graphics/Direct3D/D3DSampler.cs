using System;

namespace Chisel.Framework;

internal class D3DSampler : Disposable, ISampler
{
    public float DetailBias { get; }
    public SamplerFilterMode FilterMode { get; }
    public SamplerWrapMode WrapMode { get; }

    public D3DSampler(float bias, SamplerFilterMode filter, SamplerWrapMode wrap)
    {
        DetailBias = bias;
        FilterMode = filter;
        WrapMode = wrap;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            
        }
    }
}
