using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

internal class GLRenderTarget : Disposable, IRenderTarget
{
    public IImage[]? Color { get; }
    public IImage? DepthStencil { get; }
    internal uint Handle { get; }

    GL gl;

    public GLRenderTarget(GL gl, uint handle, IImage[]? color, IImage? depthStencil)
    {
        this.gl = gl;
        Handle = handle;
        Color = color;
        DepthStencil = depthStencil;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteFramebuffer(Handle);
        }
    }
}