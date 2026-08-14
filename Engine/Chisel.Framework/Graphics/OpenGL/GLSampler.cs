using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

internal class GLSampler : Disposable, ISampler
{
    public float DetailBias { get; }
    public SamplerFilterMode FilterMode { get; }
    public SamplerWrapMode WrapMode { get; }
    internal uint Handle { get; }

    GL gl;

    public GLSampler(GL gl, uint handle, float bias, SamplerFilterMode filter, SamplerWrapMode wrap)
    {
        this.gl = gl;
        Handle = handle;
        DetailBias = bias;
        FilterMode = filter;
        WrapMode = wrap;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteSampler(Handle);
        }
    }
}