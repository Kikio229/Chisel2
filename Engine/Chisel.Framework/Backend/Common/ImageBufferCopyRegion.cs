using System;

namespace Chisel.Framework;

public struct ImageBufferCopyRegion
{
    public uint Width;
    public uint Height;
    public uint ImageMipLevel;
    public ulong BufferOffset;
    public int OffsetX;
    public int OffsetY;

    public ImageBufferCopyRegion()
    {
        Width = 0;
        Height = 0;
        ImageMipLevel = 0;
        BufferOffset = 0;
        OffsetX = 0;
        OffsetY = 0;
    }
}