using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public class RenderTarget2D : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public uint SampleCount { get; }
    public ImageFormat ColorFormat { get; }
    public ImageFormat? DepthFormat { get; }
    public Texture2D ColorTexture { get; }   // always single-sample - what you actually sample from
    public Texture2D? DepthTexture { get; }  // null for MSAA targets right now, see note below

    Texture2D? msaaColor;
    Texture2D? msaaDepth;
    internal IRenderTarget Target { get; }   // what Begin()/End() actually bind
    IGraphicsDevice device;
    bool disposedValue;

    public RenderTarget2D(IGraphicsDevice device, int width, int height,
        ImageFormat colorFormat = ImageFormat.R8G8B8A8UNorm,
        ImageFormat? depthFormat = ImageFormat.D24UNormS8UInt,
        uint sampleCount = 1)
    {
        this.device = device;
        Width = width;
        Height = height;
        SampleCount = sampleCount;
        ColorFormat = colorFormat;
        DepthFormat = depthFormat;

        if (sampleCount > 1)
        {
            msaaColor = new Texture2D(device, width, height, colorFormat, ImageUsage.RenderTarget, sampleCount);
            ColorTexture = new Texture2D(device, width, height, colorFormat, ImageUsage.Sampled);

            if (depthFormat.HasValue)
            {
                msaaDepth = new Texture2D(device, width, height, depthFormat.Value, ImageUsage.DepthStencil, sampleCount);
            }

            Target = device.CreateRenderTarget(new RenderTargetDescription
            {
                Color = new IImage[] { msaaColor.Image },
                DepthStencil = msaaDepth?.Image,
            });
        }
        else
        {
            ColorTexture = new Texture2D(device, width, height, colorFormat, ImageUsage.Sampled | ImageUsage.RenderTarget);

            if (depthFormat.HasValue)
            {
                DepthTexture = new Texture2D(device, width, height, depthFormat.Value, ImageUsage.DepthStencil);
            }

            Target = device.CreateRenderTarget(new RenderTargetDescription
            {
                Color = new IImage[] { ColorTexture.Image },
                DepthStencil = DepthTexture?.Image,
            });
        }
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
                ColorTexture.Dispose();
                DepthTexture?.Dispose();
                msaaColor?.Dispose();
                msaaDepth?.Dispose();
                if (Target is IDisposable disposableTarget) disposableTarget.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}