using System;

namespace Chisel.Framework;

public struct BufferCopyRegion
{
    public ulong Size;
    public ulong SrcOffset;
    public ulong DstOffset;

    public BufferCopyRegion()
    {
        Size = 0;
        SrcOffset = 0;
        DstOffset = 0;
    }
}
