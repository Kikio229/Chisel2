using System;

namespace Chisel.Framework;

public interface IImage
{
    uint Width { get; }
    uint Height { get; }
    uint MipLevels { get; }
    ImageFormat Format { get; }
    ImageUsage Usage { get; }
}
