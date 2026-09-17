using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public class RenderTarget2D : IDisposable
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public uint SampleCount { get; }
    public ImageFormat ColorFormat { get; }
    public ImageFormat? DepthFormat { get; }
    public Texture2D ColorTexture { get; private set; }
    public Texture2D? DepthTexture { get; private set; }

    Texture2D? msaaColor;
    Texture2D? msaaDepth;
    internal IRenderTarget Target { get; private set; }
    IGraphicsDevice device;
    bool disposedValue;

    public RenderTarget2D(IGraphicsDevice device, int width, int height,
        ImageFormat colorFormat = ImageFormat.R8G8B8A8UNorm,
        ImageFormat? depthFormat = ImageFormat.D24UNormS8UInt,
        uint sampleCount = 1)
    {
        this.device = device;
        SampleCount = sampleCount;
        ColorFormat = colorFormat;
        DepthFormat = depthFormat;

        BuildResources(width, height);
    }
    // we are stupid lol
    public void Resize(int width, int height)
    {
        if (width == Width && height == Height)
        {
            return;
        }

        DisposeResources();
        BuildResources(width, height);
    }

    void BuildResources(int width, int height)
    {
        Width = width;
        Height = height;

        if (SampleCount > 1)
        {
            msaaColor = new Texture2D(device, width, height, ColorFormat, ImageUsage.RenderTarget, SampleCount);
            ColorTexture = new Texture2D(device, width, height, ColorFormat, ImageUsage.Sampled);

            if (DepthFormat.HasValue)
            {
                msaaDepth = new Texture2D(device, width, height, DepthFormat.Value, ImageUsage.DepthStencil, SampleCount);
            }

            Target = device.CreateRenderTarget(new RenderTargetDescription
            {
                Color = new IImage[] { msaaColor.Image },
                DepthStencil = msaaDepth?.Image,
            });
        }
        else
        {
            ColorTexture = new Texture2D(device, width, height, ColorFormat, ImageUsage.Sampled | ImageUsage.RenderTarget);

            if (DepthFormat.HasValue)
            {
                DepthTexture = new Texture2D(device, width, height, DepthFormat.Value, ImageUsage.DepthStencil);
            }

            Target = device.CreateRenderTarget(new RenderTargetDescription
            {
                Color = new IImage[] { ColorTexture.Image },
                DepthStencil = DepthTexture?.Image,
            });
        }
    }

    void DisposeResources()
    {
        ColorTexture.Dispose();
        DepthTexture?.Dispose();
        msaaColor?.Dispose();
        msaaDepth?.Dispose();
        if (Target is IDisposable disposableTarget) disposableTarget.Dispose();

        DepthTexture = null;
        msaaColor = null;
        msaaDepth = null;
    }

    public void Begin() => device.BeginDrawing(Target);

    public void End()
    {
        device.EndDrawing();

        if (SampleCount > 1)
        {
            device.ResolveImage(msaaColor!.Image, ColorTexture.Image);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                DisposeResources();
            }
            disposedValue = true;
        }
    }

    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}