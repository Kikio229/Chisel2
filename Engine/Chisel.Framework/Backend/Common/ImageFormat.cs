using System;

namespace Chisel.Framework;

public enum ImageFormat
{
    Unknown = 0,
    R8UNorm,
    R8G8UNorm,
    R8G8B8A8UNorm,
    R8G8B8A8UNormSrgb,
    R16UNorm,
    R16G16UNorm,
    R16G16B16A16UNorm,
    R16Float,
    R16G16Float,
    R16G16B16A16Float,
    R32Float,
    R32G32Float,
    R32G32B32Float,
    R32G32B32A32Float,
    R32UInt,
    R32G32UInt,
    R32G32B32UInt,
    R32G32B32A32UInt,
    D16UNorm,
    D24UNormS8UInt,
    D32Float,
    D32FloatS8UInt,
}