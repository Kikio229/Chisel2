using System;

namespace Chisel.Framework;

public enum GraphicsDepthMode
{
    Disabled = 0,
    Less,
    LessOrEqual,
    Equal,
    Greater,
    GreaterOrEqual,
    Always,
    Never,
}
