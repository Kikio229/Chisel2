using System;

namespace Chisel.Framework;

public struct BufferDescription
{
    public ulong Size;
    public BufferType Type;
    public BufferUsage Usage;

    public BufferDescription()
    {
        Size = 0;
        Type = BufferType.GpuOnly;
        Usage = BufferUsage.None;
    }
}