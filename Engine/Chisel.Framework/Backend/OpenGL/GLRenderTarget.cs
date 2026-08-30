using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLRenderTarget : Disposable, IRenderTarget
{
    public IImage[]? Color => (IImage[]?)ColorInternal;
    public IImage? DepthStencil => (IImage?)DepthStencilInternal;

    internal uint Handle { get; }
    public GLImage[]? ColorInternal { get; set; }
    public GLImage? DepthStencilInternal { get; set; }

    private readonly GL _gl;

    public GLRenderTarget(GL gl, GLImage[]? color, GLImage? depthStencil)
    {
        _gl = gl;
        Handle = _gl.GenFramebuffer();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        if (color != null)
        {
            for (int i = 0; i < color.Length; i++)
            {
                _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, color[i].Target, color[i].Handle, 0);
            }
        }

        if (depthStencil != null)
        {
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, depthStencil.Target, depthStencil.Handle, 0);
        }

        GLEnum status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        
        if (status != GLEnum.FramebufferComplete)
        {
            _gl.DeleteFramebuffer(Handle);
            throw new InvalidOperationException("Framebuffer incomplete: " + status);
        }

        ColorInternal = color;
        DepthStencilInternal = depthStencil;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteFramebuffer(Handle);
        }
    }
}