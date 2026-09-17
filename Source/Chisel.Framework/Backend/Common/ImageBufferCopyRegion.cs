using System;

namespace Chisel.Framework;

public struct ImageBufferCopyRegion
{
    public uint Width;
    public uint Height;
    public int DstOffsetX;
    public int DstOffsetY;
    public uint ImgMipLevel;
    public ulong BuffOffset;

    public ImageBufferCopyRegion()
    {
        Width = 0;
        Height = 0;
        DstOffsetX = 0; 
        DstOffsetY = 0;
        ImgMipLevel = 0;
        BuffOffset = 0;
    }
}