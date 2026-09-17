using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLBuffer : Disposable, IBuffer
{
    public ulong Size { get; }
    public BufferType Type { get; }
    public BufferUsage Usage { get; }

    internal uint Handle { get; }

    private readonly GL _gl;

    public unsafe GLBuffer(GL gl, ulong size, BufferType type, BufferUsage usage)
    {
        _gl = gl;
        Handle = _gl.GenBuffer();

        BufferUsageARB usageHint = GLUtilities.GetNativeBufferUsage(type);
        BufferTargetARB target = GLUtilities.GetNativeBufferTarget(usage);
        _gl.BindBuffer(target, Handle);
        _gl.BufferData(target, (nuint)size, null, usageHint);

        Size = size;
        Type = type;
        Usage = usage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteBuffer(Handle);
        }
    }
}