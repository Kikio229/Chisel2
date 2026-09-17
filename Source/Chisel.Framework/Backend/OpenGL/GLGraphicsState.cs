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

    // This exists solely to allow for dummy handles
    public GLGraphicsState()
    {
        Handle = 0;
        _gl = new GL(null);
    }

    public GLGraphicsState(GL gl, GLShader? vtxShader, GLShader? pixShader, ImageFormat[]? colorFormats, ImageFormat? depthStencilFormat,
        GraphicsTopology topology, GraphicsDepthMode depthMode, GraphicsBlendMode blendMode, GraphicsCullMode cullMode, GraphicsFillMode fillMode,
        VertexLayoutDescription vtxLayout, bool depthWrite, uint sampleCount)
    {
        _gl = gl;

        Handle = _gl.CreateProgram();

        if (vtxShader != null)
        {
            _gl.AttachShader(Handle, vtxShader.Handle);
        }

        if (pixShader != null)
        {
            _gl.AttachShader(Handle, pixShader.Handle);
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
        BindReflectedSlots(Handle, vtxShader!);
        BindReflectedSlots(Handle, pixShader!);
        _gl.UseProgram(0);
       
        (bool depthEnabled, DepthFunction depthFunc) = GLUtilities.GetNativeDepthMode(depthMode);
        (bool blendEnabled, BlendingFactor src, BlendingFactor dst, BlendEquationModeEXT eq) = GLUtilities.GetNativeBlendMode(blendMode);
        (bool cullEnabled, TriangleFace cullFace) = GLUtilities.GetNativeCullMode(cullMode);

        Topology = GLUtilities.GetNativeTopologyMode(topology);
        DepthFunc = depthFunc;
        BlendSrcFactor = src;
        BlendDstFactor = dst;
        BlendEquation = eq;
        CullFace = cullFace;
        FillMode = GLUtilities.GetNativeFillMode(fillMode);

        DepthTestEnabled = depthEnabled;
        DepthWriteEnabled = depthWrite;
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