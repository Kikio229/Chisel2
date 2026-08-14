using System;
using System.Collections.Generic;
using System.Linq;
using Chisel.Resource;

namespace Chisel.Framework;
public class ShaderPass : IDisposable
{
    IGraphicsDevice device;
    IShader[] stages;
    ConstantBuffer[] buffers;
    bool disposedValue;

    public ShaderProgramParameterCollection Parameters { get; } = new ShaderProgramParameterCollection();
    public IEnumerable<IShader> Stages => stages;

    public ShaderPass(IGraphicsDevice device, params IShader[] shaderStages)
    {
        this.device = device;
        stages = shaderStages;
        LinkAndReflect();
    }

    // This confuses the hell out of me
    void LinkAndReflect()
    {
        Dictionary<string, ConstantBuffer> buffersByName = new Dictionary<string, ConstantBuffer>();

        foreach (IShader stage in stages)
        {
            foreach (ConstantBufferReflection cbuffer in stage.Reflection.ConstantBuffers)
            {
                if (!buffersByName.TryGetValue(cbuffer.Name, out ConstantBuffer buffer))
                {
                    buffer = new ConstantBuffer(cbuffer.Name, cbuffer.Slot, cbuffer.SizeInBytes, device);
                    buffersByName.Add(cbuffer.Name, buffer);

                    foreach (ConstantBufferMemberReflection member in cbuffer.Members)
                    {
                        Parameters.Add(new ShaderParameter(member.Name, buffer, member.Offset));
                    }
                }
            }

            foreach (ResourceReflection image in stage.Reflection.Images)
            {
                if (!Parameters.TryGet(image.Name, out _))
                {
                    Parameters.Add(new ShaderParameter(image.Name, ShaderParameterKind.Image, image.Slot, device));
                }
            }

            foreach (ResourceReflection sampler in stage.Reflection.Samplers)
            {
                if (!Parameters.TryGet(sampler.Name, out _))
                {
                    Parameters.Add(new ShaderParameter(sampler.Name, ShaderParameterKind.Sampler, sampler.Slot, device));
                }
            }
        }

        buffers = buffersByName.Values.ToArray();
    }
    public void Apply()
    {
        foreach (ConstantBuffer buffer in buffers)
        {
            buffer.FlushAndBind();
        }

        foreach (ShaderParameter parameter in Parameters)
        {
            // No-op for PushConstant/BufferMember kinds; turns any deferred SetValue(IImage)/
            // SetValue(ISampler) into a real device.BindImage/BindSampler call for this draw's
            // descriptor block
            parameter.FlushBinding();
        }
    }

    // For reading at runtime
    public bool TryGetStage(ShaderStage stage, out IShader shader)
    {
        foreach (IShader candidate in stages)
        {
            if ((candidate.Stage & stage) != 0)
            {
                shader = candidate;
                return true;
            }
        }

        shader = null;
        return false;
    }

    public IShader GetStage(ShaderStage stage)
    {
        if (TryGetStage(stage, out IShader shader))
        {
            return shader;
        }

        throw new InvalidOperationException("Shader program has no " + stage + " stage.");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (IShader stage in stages)
                {
                    if (stage is IDisposable disposableStage)
                    {
                        disposableStage.Dispose();
                    }
                }

                foreach (ConstantBuffer buffer in buffers)
                {
                    buffer.Dispose();
                }
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}