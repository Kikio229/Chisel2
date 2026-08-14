using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Skia;

public interface ISkiaSurface : IDisposable
{
    int Width { get; }
    int Height { get; }
    SKCanvas Canvas { get; }
    Texture2D Texture { get; }

    void Invalidate();

    void PrepareForDrawing();

    void Flush();
}
