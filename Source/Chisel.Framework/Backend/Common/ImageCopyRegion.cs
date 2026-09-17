using System;

namespace Chisel.Framework;

public struct ImageCopyRegion
{
    public uint Width;
    public uint Height;
    public int SrcOffsetX;
    public int SrcOffsetY;
    public uint SrcMipLevel;
    public int DstOffsetX;
    public int DstOffsetY;
    public uint DstMipLevel;

    public ImageCopyRegion()
    {
        Width = 0;
        Height = 0;
        SrcOffsetX = 0;
        SrcOffsetY = 0;
        SrcMipLevel = 0;
        DstOffsetX = 0;
        DstOffsetY = 0;
        DstMipLevel = 0;
    }
}