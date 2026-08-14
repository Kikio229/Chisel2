using System;
using System.Runtime.InteropServices;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D12;
using static Vortice.Win32.Graphics.Direct3D12.Apis;

namespace Chisel.Framework;

internal class D3DComputeState : Disposable, IGraphicsState
{
    internal unsafe ID3D12PipelineState* PipelineState { get; set; }
    internal unsafe ID3D12RootSignature* RootSignature { get; set; }

    public unsafe D3DComputeState(ID3D12Device* device, D3DShader cmpShader)
    {
        // Root signature creation

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
                string message = (errors != null) ? Marshal.PtrToStringAnsi((nint)errors->GetBufferPointer()) ?? "Unknown root signature error!" : "Unknown root signature error!";
                throw new InvalidOperationException($"Failed to serialize D3D graphics root signature: {message}");
            }

            fixed (Guid* gptr = &ID3D12RootSignature.IID_ID3D12RootSignature)
            {
                if (device->CreateRootSignature(0, serialized->GetBufferPointer(), serialized->GetBufferSize(), gptr, (void**)&rootSig) != HResult.Ok)
                {
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

        ID3D12PipelineState* pipeState;
        ComputePipelineStateDescription pipeDesc = new ComputePipelineStateDescription()
        {
            pRootSignature = rootSig,
        };

        fixed (byte* cptr = cmpShader.Bytecode)
        {
            pipeDesc.CS = new ShaderBytecode(cptr, (nuint)cmpShader.Bytecode.Length);
        }

        fixed (Guid* gptr = &ID3D12PipelineState.IID_ID3D12PipelineState)
        {
            if (device->CreateComputePipelineState(&pipeDesc, gptr, (void**)&pipeState) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to create D3D graphics pipeline state!");
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
}
