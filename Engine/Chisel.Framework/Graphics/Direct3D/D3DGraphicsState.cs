using Chisel.Resource;
using Silk.NET.OpenGL;
using System;
using System.Runtime.InteropServices;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D12;
using Vortice.Win32.Graphics.Dxgi;
using Vortice.Win32.Graphics.Dxgi.Common;
using static Vortice.Win32.Graphics.Direct3D12.Apis;

namespace Chisel.Framework;

internal class D3DGraphicsState : Disposable, IGraphicsState
{
    internal unsafe ID3D12PipelineState* PipelineState { get; set; }
    internal unsafe ID3D12RootSignature* RootSignature { get; set; }
    internal PrimitiveTopology Topology { get; }

    public unsafe D3DGraphicsState(ID3D12Device* device, D3DShader? vtxShader, D3DShader? pixShader, ImageFormat[]? colorFormats, ImageFormat? depthStencilFormat,
        GraphicsTopology topology, GraphicsDepthMode depthMode, GraphicsBlendMode blendMode, GraphicsCullMode cullMode, GraphicsFillMode fillMode,
        VertexLayoutDescription vtxLayout, bool depthWrite, uint sampleCount)
    {
        Topology = GetPrimitiveTopology(topology);
        colorFormats ??= new[] { ImageFormat.R8G8B8A8UNorm };

        // Root signature creation. Both the DescriptorRanges and the RootParameter array they're
        // pointed at by must live for the whole D3D12SerializeRootSignature call below, so they're
        // stackalloc'd right here in the constructor's own frame, not in a helper method whose frame
        // would be gone by the time Serialize actually reads them.
        DescriptorRange* ranges = stackalloc DescriptorRange[4];
        RootParameter* parameters = stackalloc RootParameter[5];
        FillStaticRootParameters(parameters, ranges);

        RootSignatureDescription rootDesc = new RootSignatureDescription()
        {
            NumParameters = 5,
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout,
            pParameters = parameters,
            pStaticSamplers = null
        };

        ID3DBlob* serialized = default;
        ID3DBlob* errors = default;
        ID3D12RootSignature* rootSig;

        try
        {
            if (D3D12SerializeRootSignature(&rootDesc, RootSignatureVersion.V1_0, &serialized, &errors) != HResult.Ok)
            {
                DumpDebugMessages(device);
                string message = (errors != null) ? Marshal.PtrToStringAnsi((nint)errors->GetBufferPointer()) ?? "Unknown root signature error!" : "Unknown root signature error!";
                throw new InvalidOperationException($"Failed to serialize D3D graphics root signature: {message}");
            }

            fixed (Guid* gptr = &ID3D12RootSignature.IID_ID3D12RootSignature)
            {
                if (device->CreateRootSignature(0, serialized->GetBufferPointer(), serialized->GetBufferSize(), gptr, (void**)&rootSig) != HResult.Ok)
                {
                    DumpDebugMessages(device);
                    throw new InvalidOperationException("Failed to create valid D3D graphics root signature!");
                }
            }
        }
        finally
        {
            if (errors != null) errors->Release();
            if (serialized != null) serialized->Release();
        }


        // Pipeline state creation

        if (colorFormats.Length > 8)
        {
            DumpDebugMessages(device);
            throw new ArgumentException("D3D only supports up to 8 attachments on a single target!", nameof(colorFormats));
        }

        Format[] rtFormats = new Format[8];
        Format dsFormat = depthStencilFormat.HasValue ? D3DImage.GetDxgiFormatFromImage(depthStencilFormat.Value) : Format.Unknown;

        for (int i = 0; i < colorFormats.Length; i++)
        {
            rtFormats[i] = D3DImage.GetDxgiFormatFromImage(colorFormats[i]);
        }

        uint effectiveSampleCount = sampleCount == 0 ? 1 : sampleCount;

        GraphicsPipelineStateDescription pipeDesc = new GraphicsPipelineStateDescription()
        {
            pRootSignature = rootSig,
            BlendState = GetBlendDescription(blendMode),
            RasterizerState = GetRasterizerDescription(cullMode, fillMode, effectiveSampleCount),
            DepthStencilState = GetDepthStencilDescription(depthMode, depthWrite),
            PrimitiveTopologyType = GetPrimitiveTopologyFromTopology(topology),
            NumRenderTargets = (uint)colorFormats.Length,
            RTVFormats = new GraphicsPipelineStateDescription.RTVFormats__FixedBuffer()
            {
                e0 = rtFormats[0],
                e1 = rtFormats[1],
                e2 = rtFormats[2],
                e3 = rtFormats[3],
                e4 = rtFormats[4],
                e5 = rtFormats[5],
                e6 = rtFormats[6],
                e7 = rtFormats[7],
            },
            DSVFormat = dsFormat,
            SampleDesc = new SampleDescription { Count = effectiveSampleCount, Quality = 0 },
            SampleMask = uint.MaxValue,
        };

        // Weird conversion of the NICE VERTEX SYSTEM to DXs weird semantic system.
        IntPtr[] semanticNamePtrs = new IntPtr[vtxLayout.Attributes?.Length ?? 0];
        ID3D12PipelineState* pipeState;

        try
        {
            int attributeCount = vtxLayout.Attributes?.Length ?? 0;
            InputElementDescription* inputElements = stackalloc InputElementDescription[attributeCount];
            uint inputElementCount = 0;

            if (vtxShader != null && vtxLayout.Attributes != null && vtxLayout.Attributes.Length > 0)
            {
                inputElementCount = (uint)vtxLayout.Attributes.Length;

                for (int i = 0; i < inputElementCount; i++)
                {
                    VertexAttributeDescription attribute = vtxLayout.Attributes[i];

                    if (vtxShader.Reflection.Inputs == null || attribute.Location >= vtxShader.Reflection.Inputs.Length)
                    {
                        DumpDebugMessages(device);
                        throw new InvalidOperationException(
                            $"Vertex layout references location {attribute.Location}, but the vertex shader's reflected " +
                            "input signature has no matching entry. VSInput fields must be declared in ascending [[vk::location(N)]] order.");
                    }

                    VertexInputReflection semantic = vtxShader.Reflection.Inputs[attribute.Location];
                    semanticNamePtrs[i] = Marshal.StringToHGlobalAnsi(semantic.SemanticName);

                    inputElements[i] = new InputElementDescription
                    {
                        SemanticName = (byte*)semanticNamePtrs[i],
                        SemanticIndex = semantic.SemanticIndex,
                        Format = GetDxgiFormatFromVertex(attribute.Format),
                        InputSlot = 0,
                        AlignedByteOffset = (uint)attribute.Offset,
                        InputSlotClass = InputClassification.PerVertexData,
                        InstanceDataStepRate = 0,
                    };
                }
            }

            pipeDesc.InputLayout = new InputLayoutDescription
            {
                NumElements = inputElementCount,
                pInputElementDescs = inputElements,
            };

            // Every current call site (SpriteBatch, QuickDraw) always supplies both, so
            // this isn't hit in practice; if it ever needs to be, the fix belongs at the call site,
            // not here (a real single-stage-only PSO isn't a supported D3D concept).

            fixed (byte* vptr = vtxShader != null ? vtxShader.Bytecode : Array.Empty<byte>())
            fixed (byte* pptr = pixShader != null ? pixShader.Bytecode : Array.Empty<byte>())
            {
                if (vtxShader != null)
                {
                    pipeDesc.VS = new ShaderBytecode(vptr, (nuint)vtxShader.Bytecode.Length);
                }

                if (pixShader != null)
                {
                    pipeDesc.PS = new ShaderBytecode(pptr, (nuint)pixShader.Bytecode.Length);
                }

                fixed (Guid* gptr = &ID3D12PipelineState.IID_ID3D12PipelineState)
                {
                    if (device->CreateGraphicsPipelineState(&pipeDesc, gptr, (void**)&pipeState) != HResult.Ok)
                    {
                        DumpDebugMessages(device);
                        throw new InvalidOperationException("Failed to create D3D graphics pipeline state!");
                    }
                }
            }
        }
        finally
        {
            foreach (IntPtr ptr in semanticNamePtrs)
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        PipelineState = pipeState;
        RootSignature = rootSig;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (disposing)
        {
            PipelineState->Release();
            RootSignature->Release();
        }
    }

    private static Format GetDxgiFormatFromVertex(VertexElementFormat format)
    {
        return format switch
        {
            VertexElementFormat.Float1 => Format.R32Float,
            VertexElementFormat.Float2 => Format.R32G32Float,
            VertexElementFormat.Float3 => Format.R32G32B32Float,
            VertexElementFormat.Float4 => Format.R32G32B32A32Float,
            VertexElementFormat.Int1 => Format.R32Sint,
            VertexElementFormat.UInt1 => Format.R32Uint,
            VertexElementFormat.Byte1 => Format.R8Uint,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Vertex element format is unknown or invalid!")
        };
    }
    internal static unsafe void FillStaticRootParameters(RootParameter* rootParams, DescriptorRange* ranges)
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
    private static BlendDescription GetBlendDescription(GraphicsBlendMode mode)
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

    private static RasterizerDescription GetRasterizerDescription(GraphicsCullMode cullMode, GraphicsFillMode fillMode, uint sampleCount)
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
    private static DepthStencilDescription GetDepthStencilDescription(GraphicsDepthMode mode, bool allowWrite)
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
    private static PrimitiveTopologyType GetPrimitiveTopologyFromTopology(GraphicsTopology topology)
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

    // Distinct from PrimitiveTopologyType above: the PSO only cares about the *category*
    // (triangle/line/point), but IASetPrimitiveTopology at draw time needs to know list-vs-strip too.
    private static PrimitiveTopology GetPrimitiveTopology(GraphicsTopology topology)
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

    private static unsafe void DumpDebugMessages(ID3D12Device* device)
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
}
