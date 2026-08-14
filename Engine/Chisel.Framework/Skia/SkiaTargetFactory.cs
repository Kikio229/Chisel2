using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Skia;
public static class SkiaTargetFactory
{
    public static ISkiaSurface Create(IGraphicsDevice device, int width, int height)
    {
        return device.Backend switch
        {
            GraphicsBackend.OpenGL => new SkiaGpuSurface((GLGraphicsDevice)device, width, height),
            GraphicsBackend.Direct3D12 => new SkiaCpuSurface(device, width, height), // TODO: Proper DX GPU impl
            _ => throw new ArgumentOutOfRangeException(nameof(device.Backend)),
        };
    }
}