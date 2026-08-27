using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D12;
using Vortice.Win32.Graphics.Dxgi.Common;
using static Vortice.Win32.Graphics.Direct3D12.Apis;

namespace Chisel.Framework;

internal class D3DGraphicsState : Disposable, IGraphicsState
{
    internal unsafe ID3D12PipelineState* PipelineState { get; set; }
    internal unsafe ID3D12RootSignature* RootSignature { get; set; }
    internal PrimitiveTopology Topology { get; }
    public uint CbvCount { get; }
    public uint SrvCount { get; }
    public uint UavCount { get; }

    public unsafe D3DGraphicsState(ID3D12Device* device, D3DShader? vtxShader, D3DShader? pixShader, ImageFormat[]? colorFormats, ImageFormat? depthStencilFormat,
        GraphicsTopology topology, GraphicsDepthMode depthMode, GraphicsBlendMode blendMode, GraphicsCullMode cullMode, GraphicsFillMode fillMode,
        VertexLayoutDescription vtxLayout, bool depthWrite, uint sampleCount)
    {
        Topology = D3DUtilities.GetPrimitiveFromTopology(topology);
        colorFormats ??= new[] { ImageFormat.R8G8B8A8UNorm };

        if(vtxShader != null && pixShader != null)
        {
            CbvCount = ComputeCbvCount(vtxShader.Reflection, pixShader.Reflection);
            SrvCount = ComputeSrvCount(vtxShader.Reflection, pixShader.Reflection);
            UavCount = 0;
        }

        /* Root signature creation */

        // Both the DescriptorRanges and the RootParameter array they're
        // pointed at by must live for the whole D3D12SerializeRootSignature call below, so they're
        // stackalloc'd right here in the constructor's own frame
        DescriptorRange* ranges = stackalloc DescriptorRange[4];
        RootParameter* parameters = stackalloc RootParameter[5];
        D3DUtilities.GetStaticRootParameters(parameters, ranges);
        
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
                D3DUtilities.DumpInfoQueue(device);
                string message = (errors != null) ? Marshal.PtrToStringAnsi((nint)errors->GetBufferPointer()) ?? "Unknown root signature error!" : "Unknown root signature error!";
                throw new InvalidOperationException($"Failed to serialize D3D graphics root signature: {message}");
            }

            fixed (Guid* gptr = &ID3D12RootSignature.IID_ID3D12RootSignature)
            {
                HResult result = device->CreateRootSignature(0, serialized->GetBufferPointer(), serialized->GetBufferSize(), gptr, (void**)&rootSig);

                if (result != HResult.Ok)
                {
                    D3DUtilities.DumpInfoQueue(device);
                    throw new InvalidOperationException($"Failed to create valid D3D graphics root signature! HResult 0x{result:X}");
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
            D3DUtilities.DumpInfoQueue(device);
            throw new ArgumentException("D3D only supports up to 8 attachments on a single target!", nameof(colorFormats));
        }

        Format[] rtFormats = new Format[8];
        Format dsFormat = depthStencilFormat.HasValue ? D3DUtilities.GetDxgiFormatFromImage(depthStencilFormat.Value) : Format.Unknown;

        for (int i = 0; i < colorFormats.Length; i++)
        {
            rtFormats[i] = D3DUtilities.GetDxgiFormatFromImage(colorFormats[i]);
        }

        uint effectiveSampleCount = sampleCount == 0 ? 1 : sampleCount;

        GraphicsPipelineStateDescription pipeDesc = new GraphicsPipelineStateDescription()
        {
            pRootSignature = rootSig,
            BlendState = D3DUtilities.GetBlendDescription(blendMode),
            RasterizerState = D3DUtilities.GetRasterizerDescription(cullMode, fillMode, effectiveSampleCount),
            DepthStencilState = D3DUtilities.GetDepthStencilDescription(depthMode, depthWrite),
            PrimitiveTopologyType = D3DUtilities.GetPrimitiveTypeFromTopology(topology),
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
                        D3DUtilities.DumpInfoQueue(device);
                        throw new InvalidOperationException(
                            $"Vertex layout references location {attribute.Location}, but the vertex shader's reflected " +
                            "input signature has no matching entry. VSInput fields must be declared in ascending [[vk::location(N)]] order.");
                    }

                    InputReflection semantic = vtxShader.Reflection.Inputs[attribute.Location];
                    semanticNamePtrs[i] = Marshal.StringToHGlobalAnsi(semantic.Name);

                    inputElements[i] = new InputElementDescription
                    {
                        SemanticName = (byte*)semanticNamePtrs[i],
                        SemanticIndex = semantic.Index,
                        Format = D3DUtilities.GetDxgiFormatFromVertex(attribute.Format),
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
                    HResult result = device->CreateGraphicsPipelineState(&pipeDesc, gptr, (void**)&pipeState);

                    if (result != HResult.Ok)
                    {
                        D3DUtilities.DumpInfoQueue(device);
                        throw new InvalidOperationException($"Failed to create D3D graphics pipeline state! HResult 0x{result:X}");
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
    static uint MaxRegisterPlusOne(IEnumerable<uint> slots)
    {
        uint max = 0;
        bool any = false;

        foreach (uint slot in slots)
        {
            any = true;

            if (slot + 1 > max)
            {
                max = slot + 1;
            }
        }

        return any ? max : 0;
    }
    static uint ComputeCbvCount(ShaderReflection? vsRefl, ShaderReflection? psRefl)
    {
        List<uint> slots = new List<uint>();

        if (vsRefl != null)
        {
            foreach (CbufferReflection c in vsRefl.Value.Cbuffers)
            {
                slots.Add(c.Slot);
            }
        }

        if (psRefl != null)
        {
            foreach (CbufferReflection cbuffer in psRefl.Value.Cbuffers)
            {
                slots.Add(cbuffer.Slot);
            }
        }

        return MaxRegisterPlusOne(slots);
    }
    static uint ComputeSrvCount(ShaderReflection? vsRefl, ShaderReflection? psRefl)
    {
        List<uint> slots = new List<uint>();

        if (vsRefl != null)
        {
            foreach (var slot in vsRefl.Value.Samplers)
            {
                slots.Add(slot.Slot);
            }
        }

        if (psRefl != null)
        {
            foreach (var slot in psRefl.Value.Samplers)
            {
                slots.Add(slot.Slot);
            }
        }

        return MaxRegisterPlusOne(slots);
    }
    protected override unsafe void Dispose(bool disposing)
    {
        if (disposing)
        {
            PipelineState->Release();
            RootSignature->Release();
        }
    }
}
