using System;

namespace Chisel.Framework;

[Flags]
public enum GraphicsClearFlags
{
    Color = 1 << 0,
    Depth = 1 << 1,
    Stencil = 1 << 2,
}
