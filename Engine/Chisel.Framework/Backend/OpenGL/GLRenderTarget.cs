using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLRenderTarget : Disposable, IRenderTarget
{
    public IImage[]? Color { get; }
    public IImage? DepthStencil { get; }

    internal uint Handle { get; }

    private readonly GL _gl;

    public GLRenderTarget(GL gl, IImage[]? color, IImage? depthStencil)
    {
        _gl = gl;
        Handle = _gl.GenFramebuffer();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        if (color != null)
        {
            for (int i = 0; i < color.Length; i++)
            {
                GLImage colorImage = (GLImage)color[i];
                _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, colorImage.Target, colorImage.Handle, 0);
            }
        }

        if (depthStencil != null)
        {
            GLImage depthImage = (GLImage)depthStencil;
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, depthImage.Target, depthImage.Handle, 0);
        }

        GLEnum status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        
        if (status != GLEnum.FramebufferComplete)
        {
            _gl.DeleteFramebuffer(Handle);
            throw new InvalidOperationException("Framebuffer incomplete: " + status);
        }

        Color = color;
        DepthStencil = depthStencil;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteFramebuffer(Handle);
        }
    }
}