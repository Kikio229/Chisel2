using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Chisel.Framework;
public enum ShaderParameterKind
{
    PushConstant,
    BufferMember,
    Image,
    Sampler
}
public class ShaderParameter
{
    public string Name { get; }
    public ShaderParameterKind Kind { get; }
    IGraphicsDevice device;
    uint slot;
    ConstantBuffer buffer;
    int offset;

    IImage pendingImage;
    ISampler pendingSampler;

    internal ShaderParameter(string name, uint slot, IGraphicsDevice device)
    {
        Name = name;
        Kind = ShaderParameterKind.PushConstant;
        this.slot = slot;
        this.device = device;
    }
    internal ShaderParameter(string name, ConstantBuffer buffer, int offset)
    {
        Name = name;
        Kind = ShaderParameterKind.BufferMember;
        this.buffer = buffer;
        this.offset = offset;
    }
    internal ShaderParameter(string name, ShaderParameterKind kind, uint slot, IGraphicsDevice device)
    {
        Name = name;
        Kind = kind;
        this.slot = slot;
        this.device = device;
    }
    public void SetValue<T>(in T value) where T : unmanaged
    {
        if (Kind == ShaderParameterKind.PushConstant)
        {
            device.SetConstants(value, slot);
        }
        else if (Kind == ShaderParameterKind.BufferMember)
        {
            buffer.Write(offset, value);
        }
        else
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }
    }
    public void SetValue<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }
        buffer.WriteArray(offset, values);
    }
    public void SetValue(IImage image)
    {
        if (Kind != ShaderParameterKind.Image)
        {
            throw new InvalidOperationException(Name + " is not an image parameter");
        }
        pendingImage = image;
    }
    public void SetValue(ISampler sampler)
    {
        if (Kind != ShaderParameterKind.Sampler)
        {
            throw new InvalidOperationException(Name + " is not a sampler parameter");
        }
        pendingSampler = sampler;
    }

    // Called from ShaderPass.Apply(), after BindGraphicsState. Turns a deferred SetValue(...)
    // into the real device.BindImage/BindSampler call, into whichever heap block this draw was
    // just given.
    internal void FlushBinding()
    {
        if (Kind == ShaderParameterKind.Image && pendingImage != null)
        {
            device.BindImage(pendingImage, slot);
        }
        else if (Kind == ShaderParameterKind.Sampler && pendingSampler != null)
        {
            device.BindSampler(pendingSampler, slot);
        }
    }
}
public class ShaderProgramParameterCollection : IEnumerable<ShaderParameter>
{
    Dictionary<string, ShaderParameter> byName = new Dictionary<string, ShaderParameter>();
    List<ShaderParameter> ordered = new List<ShaderParameter>();
    public ShaderParameter this[string name] => byName.TryGetValue(name, out ShaderParameter parameter) ? parameter : null;
    public int Count => ordered.Count;
    public bool TryGet(string name, out ShaderParameter parameter)
    {
        return byName.TryGetValue(name, out parameter);
    }
    internal void Add(ShaderParameter parameter)
    {
        byName[parameter.Name] = parameter;
        ordered.Add(parameter);
    }
    public IEnumerator<ShaderParameter> GetEnumerator()
    {
        foreach (ShaderParameter parameter in ordered)
        {
            yield return parameter;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}