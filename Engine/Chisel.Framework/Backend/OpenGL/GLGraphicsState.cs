using System;
using Silk.NET.OpenGL;

namespace Chisel.Framework;

internal class GLGraphicsState : Disposable, IGraphicsState
{
    internal uint Handle { get; set; } // We need to make a dummy default state later on
    internal PrimitiveType Topology { get; }
    internal DepthFunction DepthFunc { get; }
    internal BlendingFactor BlendSrcFactor { get; }
    internal BlendingFactor BlendDstFactor { get; }
    internal BlendEquationModeEXT BlendEquation { get; }
    internal TriangleFace CullFace { get; }
    internal PolygonMode FillMode { get; }
    internal bool DepthTestEnabled { get; }
    internal bool DepthWriteEnabled { get; }
    internal bool BlendEnabled { get; }
    internal bool CullEnabled { get; }

    private readonly GL _gl;

    public GLGraphicsState(GL gl, GraphicsStateDescription desc, bool defaultState)
    {
        _gl = gl;
        Handle = 0;

        if (!defaultState)
        {
            Handle = _gl.CreateProgram();

            GLShader vert = (GLShader)desc.VertexShader!;
            GLShader frag = (GLShader)desc.PixelShader!;

            if (vert != null)
            {
                _gl.AttachShader(Handle, vert.Handle);
            }

            if (frag != null)
            {
                _gl.AttachShader(Handle, frag.Handle);
            }

            _gl.LinkProgram(Handle);
            _gl.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out int linkStatus);

            if (linkStatus == 0)
            {
                string log = _gl.GetProgramInfoLog(Handle);
                _gl.DeleteProgram(Handle);
                throw new InvalidOperationException("Failed to link GL program: " + log);
            }

            // Apparently GL is weird, so we have to do whatever tf this is:
            _gl.UseProgram(Handle);
            BindReflectedSlots(Handle, vert!);
            BindReflectedSlots(Handle, frag!);
            _gl.UseProgram(0);
        }

        (bool depthEnabled, DepthFunction depthFunc) = GLUtilities.GetNativeDepthMode(desc.DepthMode);
        (bool blendEnabled, BlendingFactor src, BlendingFactor dst, BlendEquationModeEXT eq) = GLUtilities.GetNativeBlendMode(desc.BlendMode);
        (bool cullEnabled, TriangleFace cullFace) = GLUtilities.GetNativeCullMode(desc.CullMode);

        Topology = GLUtilities.GetNativeTopologyMode(desc.Topology);
        DepthFunc = depthFunc;
        BlendSrcFactor = src;
        BlendDstFactor = dst;
        BlendEquation = eq;
        CullFace = cullFace;
        FillMode = GLUtilities.GetNativeFillMode(desc.FillMode);

        DepthTestEnabled = depthEnabled;
        DepthWriteEnabled = desc.AllowDepthWrite;
        BlendEnabled = blendEnabled;
        CullEnabled = cullEnabled;
    }

    private void BindReflectedSlots(uint handle, IShader shader)
    {
        if (shader == null)
        {
            return;
        }

        ShaderReflection reflection = shader.Reflection;

        // We have to kinda hack the uniforms
        foreach (CbufferReflection b in reflection.Cbuffers)
        {
            uint blockIndex = _gl.GetUniformBlockIndex(handle, b.Name);

            // I think that's the error code anyway
            if (blockIndex != uint.MaxValue)
            {
                _gl.UniformBlockBinding(handle, blockIndex, b.Slot);
            }
        }

        foreach (ResourceReflection s in reflection.Samplers)
        {
            string name = s.CompiledName ?? s.Name;
            int location = _gl.GetUniformLocation(handle, name);

            if (Game.Instance?.Window.IsDebug ?? false)
            {
                Logger.AppendInfo($"Bound to named GL sampler: {s.CompiledName}");
            }

            if (location >= 0)
            {
                _gl.Uniform1(location, (int)s.Slot);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gl.DeleteProgram(Handle);
        }
    }
}