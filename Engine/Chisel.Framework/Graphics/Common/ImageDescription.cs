using System;

namespace Chisel.Framework;

public struct ImageDescription
{
    public uint Width;
    public uint Height;
    public uint MipLevels;
    public ImageFormat Format;
    public ImageUsage Usage;
    public uint SampleCount;

    public ImageDescription()
    {
        Width = 0;
        Height = 0;
        Format = ImageFormat.R8G8B8A8UNorm; // TODO
        Usage = ImageUsage.Sampled;
    }
}
