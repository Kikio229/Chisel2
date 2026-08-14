using System;

namespace Chisel.Framework;

public struct GraphicsStateDescription
{
    public IShader? VertexShader;
    public IShader? PixelShader;
    public ImageFormat[]? ColorFormats;
    public ImageFormat? DepthStencilFormat;
    public GraphicsTopology Topology;
    public GraphicsDepthMode DepthMode;
    public GraphicsBlendMode BlendMode;
    public GraphicsCullMode CullMode;
    public GraphicsFillMode FillMode;
    public VertexLayoutDescription VertexLayout;
    public bool AllowDepthWrite;

    public uint SampleCount; // matches whatever render target this state will actually be drawn into

    public GraphicsStateDescription()
    {
        VertexShader = null;
        PixelShader = null;
        ColorFormats = null;
        DepthStencilFormat = null;
        Topology = GraphicsTopology.TriangleList;
        DepthMode = GraphicsDepthMode.Disabled;
        BlendMode = GraphicsBlendMode.Opaque;
        FillMode = GraphicsFillMode.Solid;
        AllowDepthWrite = false;
        SampleCount = 1;
    }
}