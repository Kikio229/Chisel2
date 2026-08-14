using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
internal class GLGraphicsState : Disposable, IGraphicsState
{
    internal uint ProgramHandle { get; }
    internal PrimitiveType Topology { get; }
    internal bool DepthTestEnabled { get; }
    internal DepthFunction DepthFunc { get; }
    internal bool DepthWriteEnabled { get; }
    internal bool BlendEnabled { get; }
    internal BlendingFactor BlendSrcFactor { get; }
    internal BlendingFactor BlendDstFactor { get; }
    internal BlendEquationModeEXT BlendEquation { get; }
    internal bool CullEnabled { get; }
    internal TriangleFace CullFace { get; }
    internal PolygonMode FillMode { get; }

    GL gl;

    public GLGraphicsState(GL gl, uint programHandle, GraphicsStateDescription description)
    {
        this.gl = gl;
        ProgramHandle = programHandle;
        Topology = TranslateTopology(description.Topology);

        (bool depthEnabled, DepthFunction depthFunc) = TranslateDepthMode(description.DepthMode);
        DepthTestEnabled = depthEnabled;
        DepthFunc = depthFunc;
        DepthWriteEnabled = description.AllowDepthWrite;

        (bool blendEnabled, BlendingFactor src, BlendingFactor dst, BlendEquationModeEXT eq) = TranslateBlendMode(description.BlendMode);
        BlendEnabled = blendEnabled;
        BlendSrcFactor = src;
        BlendDstFactor = dst;
        BlendEquation = eq;

        (bool cullEnabled, TriangleFace cullFace) = TranslateCullMode(description.CullMode);
        CullEnabled = cullEnabled;
        CullFace = cullFace;

        FillMode = TranslateFillMode(description.FillMode);
    }

    static PrimitiveType TranslateTopology(GraphicsTopology topology)
    {
        switch (topology)
        {
            case GraphicsTopology.TriangleList:
                return PrimitiveType.Triangles;
            case GraphicsTopology.TriangleStrip:
                return PrimitiveType.TriangleStrip;
            case GraphicsTopology.LineList:
                return PrimitiveType.Lines;
            case GraphicsTopology.LineStrip:
                return PrimitiveType.LineStrip;
            case GraphicsTopology.PointList:
                return PrimitiveType.Points;
            default:
                throw new ArgumentOutOfRangeException(nameof(topology));
        }
    }

    static (bool, DepthFunction) TranslateDepthMode(GraphicsDepthMode mode)
    {
        switch (mode)
        {
            case GraphicsDepthMode.Disabled:
                return (false, DepthFunction.Always);
            case GraphicsDepthMode.Less:
                return (true, DepthFunction.Less);
            case GraphicsDepthMode.LessOrEqual:
                return (true, DepthFunction.Lequal);
            case GraphicsDepthMode.Equal:
                return (true, DepthFunction.Equal);
            case GraphicsDepthMode.Greater:
                return (true, DepthFunction.Greater);
            case GraphicsDepthMode.GreaterOrEqual:
                return (true, DepthFunction.Gequal);
            case GraphicsDepthMode.Always:
                return (true, DepthFunction.Always);
            case GraphicsDepthMode.Never:
                return (true, DepthFunction.Never);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    static (bool, BlendingFactor, BlendingFactor, BlendEquationModeEXT) TranslateBlendMode(GraphicsBlendMode mode)
    {
        switch (mode)
        {
            case GraphicsBlendMode.Opaque:
                return (false, BlendingFactor.One, BlendingFactor.Zero, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Alpha:
                return (true, BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Additive:
                return (true, BlendingFactor.SrcAlpha, BlendingFactor.One, BlendEquationModeEXT.FuncAdd);
            case GraphicsBlendMode.Multiply:
                return (true, BlendingFactor.DstColor, BlendingFactor.Zero, BlendEquationModeEXT.FuncAdd);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    static (bool, TriangleFace) TranslateCullMode(GraphicsCullMode mode)
    {
        switch (mode)
        {
            case GraphicsCullMode.None:
                return (false, TriangleFace.Back);
            case GraphicsCullMode.Front:
                return (true, TriangleFace.Front);
            case GraphicsCullMode.Back:
                return (true, TriangleFace.Back);
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    static PolygonMode TranslateFillMode(GraphicsFillMode mode)
    {
        switch (mode)
        {
            case GraphicsFillMode.Solid:
                return PolygonMode.Fill;
            case GraphicsFillMode.Wireframe:
                return PolygonMode.Line;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteProgram(ProgramHandle);
        }
    }
}