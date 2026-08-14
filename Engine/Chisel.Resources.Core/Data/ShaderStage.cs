using System;
using System.Collections.Generic;
using System.Text;

namespace Chisel.Resource;

// We'll make these flags so that we can actually have params for multiple at once
[Flags]
public enum ShaderStage
{
    None = 1 << 0,
    Vertex = 1 << 1,
    Pixel = 1 << 2,
    Compute = 1 << 3
}