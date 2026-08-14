using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Win32;
using Vortice.Win32.Graphics.Direct3D;
using Vortice.Win32.Graphics.Direct3D.Dxc;
using Vortice.Win32.Graphics.Direct3D12;
using static Vortice.Win32.Graphics.Direct3D.Dxc.Apis;

namespace Chisel.Resource.Builder;

static unsafe class DxcHelper
{
    public static byte[] Compile(string source, string entry, string profile, bool spirv)
    {
        IDxcCompiler3* compiler;
        fixed (Guid* gptr = &IDxcCompiler3.IID_IDxcCompiler3)
        {
            if (DxcCreateInstance(CLSID_DxcCompiler, gptr, (void**)&compiler) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to create DXC compiler instance.");
            }
        }

        byte[] sourceBytes = Encoding.UTF8.GetBytes(source);

        List<string> argList = new List<string> { "-E", entry, "-T", profile, "-HV", "2021" };

        if (spirv)
        {
            argList.Add("-spirv");
        }

        nint[] argPtrs = new nint[argList.Count];

        try
        {
            for (int i = 0; i < argList.Count; i++)
            {
                argPtrs[i] = Marshal.StringToHGlobalUni(argList[i]);
            }

            fixed (byte* sourcePtr = sourceBytes)
            fixed (nint* argsPtr = argPtrs)
            {
                DxcBuffer sourceBuffer = new DxcBuffer
                {
                    Ptr = sourcePtr,
                    Size = (nuint)sourceBytes.Length,
                    Encoding = DXC_CP_UTF8,
                };

                IDxcResult* result;
                fixed (Guid* gptr = &IDxcResult.IID_IDxcResult)
                {
                    compiler->Compile(&sourceBuffer, (char**)argsPtr, (uint)argPtrs.Length, null, gptr, (void**)&result);
                }

                HResult status;
                result->GetStatus(&status);

                if (status != HResult.Ok)
                {
                    IDxcBlobUtf8* errors = null;
                    fixed (Guid* gptr = &IDxcBlobUtf8.IID_IDxcBlobUtf8)
                    {
                        result->GetOutput(DXC_OUT_ERRORS, gptr, (void**)&errors, null);
                    }

                    string message = errors != null && errors->GetStringLength() > 0
                        ? Encoding.UTF8.GetString((byte*)errors->GetStringPointer(), (int)errors->GetStringLength())
                        : "Unknown DXC compilation error";

                    result->Release();
                    compiler->Release();
                    throw new InvalidOperationException("DXC failed to compile '" + entry + "': " + message);
                }

                IDxcBlob* bytecodeBlob;
                fixed (Guid* gptr = &IDxcBlob.IID_IDxcBlob)
                {
                    result->GetOutput(DXC_OUT_OBJECT, gptr, (void**)&bytecodeBlob, null);
                }

                byte[] bytecode = new byte[bytecodeBlob->GetBufferSize()];

                fixed (byte* dst = bytecode)
                {
                    Buffer.MemoryCopy(bytecodeBlob->GetBufferPointer(), dst, bytecode.Length, bytecode.Length);
                }

                bytecodeBlob->Release();
                result->Release();
                compiler->Release();

                return bytecode;
            }
        }
        finally
        {
            foreach (nint ptr in argPtrs)
            {
                if (ptr != 0)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }
    }

    public static ShaderReflection ReflectDxil(byte[] dxil, ShaderStage stage)
    {
        IDxcUtils* utils;
        fixed (Guid* gptr = &IDxcUtils.IID_IDxcUtils)
        {
            if (DxcCreateInstance(CLSID_DxcUtils, gptr, (void**)&utils) != HResult.Ok)
            {
                throw new InvalidOperationException("Failed to create DXC utils instance.");
            }
        }

        ID3D12ShaderReflection* shaderReflection;

        fixed (byte* dxilPtr = dxil)
        {
            DxcBuffer reflectionBuffer = new DxcBuffer
            {
                Ptr = dxilPtr,
                Size = (nuint)dxil.Length,
                Encoding = 0,
            };

            fixed (Guid* gptr = &ID3D12ShaderReflection.IID_ID3D12ShaderReflection)
            {
                utils->CreateReflection(&reflectionBuffer, gptr, (void**)&shaderReflection);
            }
        }

        ShaderDescription desc;
        shaderReflection->GetDesc(&desc);

        List<ConstantBufferReflection> constantBuffers = new List<ConstantBufferReflection>();
        List<ResourceReflection> images = new List<ResourceReflection>();
        List<ResourceReflection> samplers = new List<ResourceReflection>();
        List<VertexInputReflection> inputs = new List<VertexInputReflection>();

        for (uint i = 0; i < desc.BoundResources; i++)
        {
            ShaderInputBindDescription bindDesc;
            shaderReflection->GetResourceBindingDesc(i, &bindDesc);

            string name = new string((sbyte*)bindDesc.Name);

            switch (bindDesc.Type)
            {
                case ShaderInputType.ConstantBuffer:
                    ID3D12ShaderReflectionConstantBuffer* cb = shaderReflection->GetConstantBufferByName(bindDesc.Name);
                    ShaderBufferDescription cbDesc;
                    cb->GetDesc(&cbDesc);

                    List<ConstantBufferMemberReflection> members = new List<ConstantBufferMemberReflection>();

                    for (uint v = 0; v < cbDesc.Variables; v++)
                    {
                        ID3D12ShaderReflectionVariable* variable = cb->GetVariableByIndex(v);
                        ShaderVariableDescription varDesc;
                        variable->GetDesc(&varDesc);

                        members.Add(new ConstantBufferMemberReflection
                        {
                            Name = new string((sbyte*)varDesc.Name),
                            Offset = (int)varDesc.StartOffset,
                            SizeInBytes = (int)varDesc.Size,
                        });
                    }

                    constantBuffers.Add(new ConstantBufferReflection
                    {
                        Name = name,
                        Slot = bindDesc.BindPoint,
                        SizeInBytes = (int)cbDesc.Size,
                        Members = members.ToArray(),
                    });
                    break;

                case ShaderInputType.Texture:
                    images.Add(new ResourceReflection { Name = name, Slot = bindDesc.BindPoint });
                    break;

                case ShaderInputType.Sampler:
                    samplers.Add(new ResourceReflection { Name = name, Slot = bindDesc.BindPoint });
                    break;
            }
        }

        if (stage == ShaderStage.Vertex)
        {
            for (uint i = 0; i < desc.InputParameters; i++)
            {
                SignatureParameterDescription paramDesc;
                shaderReflection->GetInputParameterDesc(i, &paramDesc);

                inputs.Add(new VertexInputReflection
                {
                    SemanticName = new string((sbyte*)paramDesc.SemanticName),
                    SemanticIndex = paramDesc.SemanticIndex,
                });
            }
        }

        shaderReflection->Release();
        utils->Release();

        return new ShaderReflection
        {
            ConstantBuffers = constantBuffers.ToArray(),
            Images = images.ToArray(),
            Samplers = samplers.ToArray(),
            Inputs = inputs.ToArray(),
        };
    }
}