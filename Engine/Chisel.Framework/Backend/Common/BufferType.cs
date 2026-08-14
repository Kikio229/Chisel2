using System;

namespace Chisel.Framework;

public enum BufferType
{
    GpuOnly = 0,
    Upload,
    Readback
}