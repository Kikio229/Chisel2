using System;

namespace Chisel.Framework;

[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    Constant = 1 << 2,
    Storage = 1 << 3,
    Indirect = 1 << 4,
    CopySource = 1 << 5,
}