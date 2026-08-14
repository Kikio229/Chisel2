using Hexa.NET.SDL3;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Skia;
public static class SkiaGL
{
    static GRContext context;
    public static GRContext Context => context;

    public static unsafe void Init()
    {
        if (context != null)
        {
            return;
        }

        GRGlInterface glInterface = GRGlInterface.Create();

        if (glInterface == null || !glInterface.Validate())
        {
            glInterface = GRGlInterface.CreateOpenGl((name) => (nint)SDL.GLGetProcAddress(name));
        }

        context = GRContext.CreateGl(glInterface);
    }
}