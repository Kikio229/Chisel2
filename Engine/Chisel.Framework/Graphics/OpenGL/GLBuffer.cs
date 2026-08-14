using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

internal class GLBuffer : Disposable, IBuffer
{
    public ulong Size { get; }
    public BufferType Type { get; }
    public BufferUsage Usage { get; }
    internal uint Handle { get; }

    GL gl;

    public GLBuffer(GL gl, uint handle, ulong size, BufferType type, BufferUsage usage)
    {
        this.gl = gl;
        Handle = handle;
        Size = size;
        Type = type;
        Usage = usage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteBuffer(Handle);
        }
    }
}