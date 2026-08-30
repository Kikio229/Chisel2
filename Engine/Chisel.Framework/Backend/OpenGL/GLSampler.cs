using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLSampler : Disposable, ISampler
{
    public float DetailBias { get; }
    public SamplerFilterMode FilterMode { get; }
    public SamplerWrapMode WrapMode { get; }

    internal uint Handle { get; }

    private readonly GL _gl;

    public GLSampler(GL gl, float bias, SamplerFilterMode filter, SamplerWrapMode wrap)
    {
        _gl = gl;

        Handle = _gl.GenSampler();
        GLEnum wrapMode = GLUtilities.GetNativeWrapMode(wrap);
        (TextureMinFilter minFilter, TextureMagFilter magFilter) = GLUtilities.GetNativeFilterMode(filter);

        _gl.SamplerParameter(Handle, SamplerParameterI.MinFilter, (int)minFilter);
        _gl.SamplerParameter(Handle, SamplerParameterI.MagFilter, (int)magFilter);
        _gl.SamplerParameter(Handle, SamplerParameterI.WrapS, (int)wrap);
        _gl.SamplerParameter(Handle, SamplerParameterI.WrapT, (int)wrap);

        DetailBias = bias;
        FilterMode = filter;
        WrapMode = wrap;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteSampler(Handle);
        }
    }
}