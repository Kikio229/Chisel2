using System;
using System.Runtime.InteropServices;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D12;
using Vortice.Win32.Graphics.Dxgi.Common;

namespace Chisel.Framework;

internal static class D3DUtilities
{
    public static unsafe void DumpInfoQueue(ID3D12Device* device)
    {
        ID3D12InfoQueue* infoQueue;
        fixed (Guid* gptr = &ID3D12InfoQueue.IID_ID3D12InfoQueue)
        {
            if (device->QueryInterface(gptr, (void**)&infoQueue) != HResult.Ok)
            {
                return;
            }
        }

        ulong count = infoQueue->GetNumStoredMessages();
        
        for (ulong i = 0; i < count; i++)
        {
            nuint size = 0;
            infoQueue->GetMessage(i, null, &size);
            if (size == 0) continue;

            Message* message = (Message*)Marshal.AllocHGlobal((int)size);
            infoQueue->GetMessage(i, message, &size);
            Logger.AppendLog("D3D", Marshal.PtrToStringAnsi((nint)message->pDescription)!, ConsoleColor.Red, 1);
            Marshal.FreeHGlobal((nint)message);
        }

        infoQueue->ClearStoredMessages();
        infoQueue->Release();
    }

    internal static Format GetDxgiFormatFromImage(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Unknown => Format.Unknown,
            ImageFormat.R8UNorm => Format.R8Unorm,
            ImageFormat.R8G8UNorm => Format.R8G8Unorm,
            ImageFormat.R8G8B8A8UNorm => Format.R8G8B8A8Unorm,
            ImageFormat.R8G8B8A8UNormSrgb => Format.R8G8B8A8UnormSrgb,
            ImageFormat.R16UNorm => Format.R16Unorm,
            ImageFormat.R16G16UNorm => Format.R16G16Unorm,
            ImageFormat.R16G16B16A16UNorm => Format.R16G16B16A16Unorm,
            ImageFormat.R16Float => Format.R16Float,
            ImageFormat.R16G16Float => Format.R16G16Float,
            ImageFormat.R16G16B16A16Float => Format.R16G16B16A16Float,
            ImageFormat.R32Float => Format.R32Float,
            ImageFormat.R32G32Float => Format.R32G32Float,
            ImageFormat.R32G32B32Float => Format.R32G32B32Float,
            ImageFormat.R32G32B32A32Float => Format.R32G32B32A32Float,
            ImageFormat.R32UInt => Format.R32Uint,
            ImageFormat.R32G32UInt => Format.R32G32Uint,
            ImageFormat.R32G32B32UInt => Format.R32G32B32Uint,
            ImageFormat.R32G32B32A32UInt => Format.R32G32B32A32Uint,
            ImageFormat.D16UNorm => Format.D16Unorm,
            ImageFormat.D24UNormS8UInt => Format.D24UnormS8Uint,
            ImageFormat.D32Float => Format.D32Float,
            ImageFormat.D32FloatS8UInt => Format.D32FloatS8X24Uint,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Image format is unknown or invalid!")
        };
    }

    public static Format GetDxgiFormatFromVertex(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Float1 => Format.R32Float,
            VertexFormat.Float2 => Format.R32G32Float,
            VertexFormat.Float3 => Format.R32G32B32Float,
            VertexFormat.Float4 => Format.R32G32B32A32Float,
            VertexFormat.Int1 => Format.R32Sint,
            VertexFormat.UInt1 => Format.R32Uint,
            VertexFormat.Byte1 => Format.R8Uint,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Vertex element format is unknown or invalid!")
        };
    }

    public static uint GetBytesPerPixel(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8UNorm => 1,
            ImageFormat.R8G8UNorm => 2,
            ImageFormat.R8G8B8A8UNorm or ImageFormat.R8G8B8A8UNormSrgb => 4,
            ImageFormat.R16UNorm or ImageFormat.R16Float => 2,
            ImageFormat.R16G16UNorm or ImageFormat.R16G16Float => 4,
            ImageFormat.R16G16B16A16UNorm or ImageFormat.R16G16B16A16Float => 8,
            ImageFormat.R32Float or ImageFormat.R32UInt => 4,
            ImageFormat.R32G32Float or ImageFormat.R32G32UInt => 8,
            ImageFormat.R32G32B32Float or ImageFormat.R32G32B32UInt => 12,
            ImageFormat.R32G32B32A32Float or ImageFormat.R32G32B32A32UInt => 16,
            ImageFormat.D16UNorm => 2,
            ImageFormat.D24UNormS8UInt or ImageFormat.D32Float => 4,
            ImageFormat.D32FloatS8UInt => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Image format is unknown or invalid!")
        };
    }

    public static HeapType GetHeapTypeFromBuffer(BufferType type)
    {
        return type switch
        {
            BufferType.GpuOnly => HeapType.Default,
            BufferType.Upload => HeapType.Upload,
            BufferType.Readback => HeapType.Readback,
            _ => HeapType.Default
        };
    }

    public static ResourceStates GetResourceStateFromBuffer(BufferType type)
    {
        return type switch
        {
            BufferType.GpuOnly => ResourceStates.Common,
            BufferType.Upload => ResourceStates.GenericRead,
            BufferType.Readback => ResourceStates.CopyDest,
            _ => ResourceStates.Common
        };
    }

    public static BlendDescription GetBlendDescription(GraphicsBlendMode mode)
    {
        BlendDescription desc = new BlendDescription();

        desc.RenderTarget[0] = new RenderTargetBlendDescription()
        {
            BlendEnable = (mode != GraphicsBlendMode.Opaque),
            SrcBlend = mode switch
            {
                GraphicsBlendMode.Alpha => Blend.SrcAlpha,
                GraphicsBlendMode.Additive => Blend.One,
                GraphicsBlendMode.Multiply => Blend.DestColor,
                _ => Blend.One
            },
            DestBlend = mode switch
            {
                GraphicsBlendMode.Alpha => Blend.InverseSrcAlpha,
                GraphicsBlendMode.Additive => Blend.One,
                GraphicsBlendMode.Multiply => Blend.Zero,
                _ => Blend.Zero
            },
            BlendOp = BlendOperation.Add,
            SrcBlendAlpha = Blend.One,
            DestBlendAlpha = Blend.InverseSrcAlpha,
            BlendOpAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };

        return desc;
    }

    public static RasterizerDescription GetRasterizerDescription(GraphicsCullMode cullMode, GraphicsFillMode fillMode, uint sampleCount)
    {
        return new RasterizerDescription()
        {
            FillMode = fillMode switch
            {
                GraphicsFillMode.Solid => FillMode.Solid,
                GraphicsFillMode.Wireframe => FillMode.Wireframe,
                _ => throw new ArgumentOutOfRangeException(nameof(fillMode))
            },
            CullMode = cullMode switch
            {
                GraphicsCullMode.None => CullMode.None,
                GraphicsCullMode.Front => CullMode.Front,
                GraphicsCullMode.Back => CullMode.Back,
                _ => throw new ArgumentOutOfRangeException(nameof(cullMode))
            },
            FrontCounterClockwise = true,
            DepthBias = 0,
            DepthBiasClamp = 0,
            SlopeScaledDepthBias = 0,
            DepthClipEnable = true,
            MultisampleEnable = sampleCount > 1,
            AntialiasedLineEnable = false,
            ForcedSampleCount = 0,
            ConservativeRaster = ConservativeRasterizationMode.Off
        };
    }

    public static DepthStencilDescription GetDepthStencilDescription(GraphicsDepthMode mode, bool allowWrite)
    {
        DepthStencilOperationDescription noOp = new DepthStencilOperationDescription
        {
            StencilFailOp = StencilOperation.Keep,
            StencilDepthFailOp = StencilOperation.Keep,
            StencilPassOp = StencilOperation.Keep,
            StencilFunc = ComparisonFunction.Always,
        };

        if (mode == GraphicsDepthMode.Disabled)
        {
            return new DepthStencilDescription
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthFunc = ComparisonFunction.Always,
                StencilEnable = false,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,
                FrontFace = noOp,
                BackFace = noOp,
            };
        }

        return new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = (allowWrite) ? DepthWriteMask.All : DepthWriteMask.Zero,
            DepthFunc = mode switch
            {
                GraphicsDepthMode.Less => ComparisonFunction.Less,
                GraphicsDepthMode.LessOrEqual => ComparisonFunction.LessEqual,
                GraphicsDepthMode.Equal => ComparisonFunction.Equal,
                GraphicsDepthMode.Greater => ComparisonFunction.Greater,
                GraphicsDepthMode.GreaterOrEqual => ComparisonFunction.GreaterEqual,
                GraphicsDepthMode.Always => ComparisonFunction.Always,
                GraphicsDepthMode.Never => ComparisonFunction.Never,
                _ => ComparisonFunction.Always
            },
            StencilEnable = false,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = noOp,
            BackFace = noOp,
        };
    }

    public static PrimitiveTopology GetPrimitiveFromTopology(GraphicsTopology topology)
    {
        return topology switch
        {
            GraphicsTopology.TriangleList => PrimitiveTopology.TriangleList,
            GraphicsTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
            GraphicsTopology.LineList => PrimitiveTopology.LineList,
            GraphicsTopology.LineStrip => PrimitiveTopology.LineStrip,
            GraphicsTopology.PointList => PrimitiveTopology.PointList,
            _ => throw new ArgumentOutOfRangeException(nameof(topology), topology, "Topology is unknown or invalid!")
        };
    }

    public static PrimitiveTopologyType GetPrimitiveTypeFromTopology(GraphicsTopology topology)
    {
        return topology switch
        {
            GraphicsTopology.TriangleList => PrimitiveTopologyType.Triangle,
            GraphicsTopology.TriangleStrip => PrimitiveTopologyType.Triangle,
            GraphicsTopology.LineList => PrimitiveTopologyType.Line,
            GraphicsTopology.LineStrip => PrimitiveTopologyType.Line,
            GraphicsTopology.PointList => PrimitiveTopologyType.Point,
            _ => throw new ArgumentOutOfRangeException(nameof(topology), topology, "Topology is unknown or invalid!")
        };
    }

    public static unsafe void GetStaticRootParameters(RootParameter* rootParams, DescriptorRange* ranges)
    {
        ranges[0] = new DescriptorRange(DescriptorRangeType.Cbv, 16, 0, 0);
        ranges[1] = new DescriptorRange(DescriptorRangeType.Srv, 16, 0, 0);
        ranges[2] = new DescriptorRange(DescriptorRangeType.Uav, 16, 0, 0);
        ranges[3] = new DescriptorRange(DescriptorRangeType.Sampler, 16, 0, 0);

        rootParams[0] = new RootParameter() // Root constants
        {
            ParameterType = RootParameterType.T32BitConstants,
            Anonymous = new RootParameter._Anonymous_e__Union()
            {
                Constants = new RootConstants()
                {
                    ShaderRegister = 16,
                    Num32BitValues = 0,
                    RegisterSpace = 1,
                }
            },
            ShaderVisibility = ShaderVisibility.All
        };

        rootParams[1] = new RootParameter() // Constant buffer
        {
            ParameterType = RootParameterType.DescriptorTable,
            Anonymous = new RootParameter._Anonymous_e__Union()
            {
                DescriptorTable = new RootDescriptorTable()
                {
                    NumDescriptorRanges = 1,
                    pDescriptorRanges = &ranges[0]
                }
            },
            ShaderVisibility = ShaderVisibility.All
        };

        rootParams[2] = new RootParameter() // Shader resource
        {
            ParameterType = RootParameterType.DescriptorTable,
            Anonymous = new RootParameter._Anonymous_e__Union()
            {
                DescriptorTable = new RootDescriptorTable()
                {
                    NumDescriptorRanges = 1,
                    pDescriptorRanges = &ranges[1]
                }
            },
            ShaderVisibility = ShaderVisibility.All
        };

        rootParams[3] = new RootParameter() // Unordered access
        {
            ParameterType = RootParameterType.DescriptorTable,
            Anonymous = new RootParameter._Anonymous_e__Union()
            {
                DescriptorTable = new RootDescriptorTable()
                {
                    NumDescriptorRanges = 1,
                    pDescriptorRanges = &ranges[2]
                }
            },
            ShaderVisibility = ShaderVisibility.All
        };

        rootParams[4] = new RootParameter() // Sampler
        {
            ParameterType = RootParameterType.DescriptorTable,
            Anonymous = new RootParameter._Anonymous_e__Union()
            {
                DescriptorTable = new RootDescriptorTable()
                {
                    NumDescriptorRanges = 1,
                    pDescriptorRanges = &ranges[3]
                }
            },
            ShaderVisibility = ShaderVisibility.All
        };
    }
}
