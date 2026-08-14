using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Skia;

// !!!!!!!!!!!!!!!!!!!!!!!!!!
// THIS IS VERY EXPERIMENTAL
// !!!!!!!!!!!!!!!!!!!!!!!!!!
public class SkiaCpuSurface : Disposable, ISkiaSurface
{
    public int Width { get; }
    public int Height { get; }
    public Texture2D Texture { get; }
    public SKCanvas Canvas => surface.Canvas;

    SKSurface surface;
    bool dirty = true; // always draw at least once

    public SkiaCpuSurface(IGraphicsDevice device, int width, int height)
    {
        Width = width;
        Height = height;

        SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        surface = SKSurface.Create(imageInfo);

        Texture = new Texture2D(device, width, height);
    }

    public void Invalidate() => dirty = true;

    public void PrepareForDrawing()
    {
        // Nothing to reset on the CPU path 
    }

    public void Flush()
    {
        if (!dirty)
        {
            return;
        }

        surface.Canvas.Flush();

        using SKPixmap pixmap = surface.PeekPixels();
        Texture.SetData(pixmap.GetPixelSpan());

        dirty = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            surface.Dispose();
            Texture.Dispose();
        }
    }
}