using System;

namespace Chisel.Framework;

public interface IBuffer
{
    ulong Size { get; }
    BufferType Type { get; }
    BufferUsage Usage { get; }
}
