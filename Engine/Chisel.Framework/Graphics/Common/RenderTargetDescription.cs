using System;

namespace Chisel.Framework;

public struct RenderTargetDescription
{
    public IImage[]? Color;
    public IImage? DepthStencil;

    public RenderTargetDescription()
    {
        Color = null;
        DepthStencil = null;
    }
}