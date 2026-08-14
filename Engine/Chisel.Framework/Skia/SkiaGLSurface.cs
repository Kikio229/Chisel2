using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Skia;
public class SkiaGpuSurface : Disposable, ISkiaSurface
{
    const uint GL_RGBA8 = 0x8058;

    public int Width { get; }
    public int Height { get; }
    public Texture2D Texture => RenderTarget.ColorTexture;
    public SKCanvas Canvas => surface.Canvas;
    public RenderTarget2D RenderTarget { get; }

    GLGraphicsDevice device;
    SKSurface surface;
    GRBackendRenderTarget backendTarget;
    bool dirty = true;

    public SkiaGpuSurface(GLGraphicsDevice device, int width, int height)
    {
        this.device = device;
        Width = width;
        Height = height;

        RenderTarget = new RenderTarget2D(device, width, height, depthFormat: null);

        GLRenderTarget glTarget = (GLRenderTarget)RenderTarget.Target;
        GRGlFramebufferInfo fbInfo = new GRGlFramebufferInfo(glTarget.Handle, GL_RGBA8);
        backendTarget = new GRBackendRenderTarget(width, height, 0, 0, fbInfo);

        surface = SKSurface.Create(SkiaGL.Context, backendTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    public void Invalidate() => dirty = true;

    public void PrepareForDrawing()
    {
        if (!dirty)
        {
            return;
        }

        SkiaGL.Context.ResetContext();
    }

    public void Flush()
    {
        if (!dirty)
        {
            return;
        }

        surface.Canvas.Flush();
        SkiaGL.Context.Flush();
        device.EndDrawing();

        dirty = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            surface.Dispose();
            backendTarget.Dispose();
            RenderTarget.Dispose();
        }
    }
}