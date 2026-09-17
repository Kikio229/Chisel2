using System;

namespace Chisel.Framework;

public interface IRenderTarget
{
    IImage[]? Color { get; }
    IImage? DepthStencil { get; }
}
