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

    // Special overloads for the new math types because they're weird
    public void SetValue(Vector4 value)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        Span<float> values = stackalloc float[4]
        {
            value.X, value.Y, value.Z, value.W
        };

        buffer.WriteArray<float>(offset, values);
    }
    public void SetValue(Vector3 value)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        Span<float> values = stackalloc float[3]
        {
            value.X, value.Y, value.Z
        };

        buffer.WriteArray<float>(offset, values);
    }
    public void SetValue(Vector2 value)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        Span<float> values = stackalloc float[2]
        {
            value.X, value.Y
        };

        buffer.WriteArray<float>(offset, values);
    }
    public void SetValue(Matrix value)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        Span<float> values = stackalloc float[16]
        {
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44
        };

        buffer.WriteArray<float>(offset, values);
    }
    public void SetValue(ReadOnlySpan<Vector2> values)
    {
        if (Kind != ShaderParameterKind.BufferMember)
            throw new InvalidOperationException(Name + " is not a value parameter");
        buffer.WriteArray(offset, values);
    }
    public void SetValue(ReadOnlySpan<Vector3> values)
    {
        if (Kind != ShaderParameterKind.BufferMember)
            throw new InvalidOperationException(Name + " is not a value parameter");
        buffer.WriteArray(offset, values);
    }

    public void SetValue(ReadOnlySpan<Vector4> values)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        int count = values.Length;
        Span<float> data = count <= 64
            ? stackalloc float[count * 4]
            : new float[count * 4];

        for (int i = 0; i < count; i++)
        {
            data[(i * 4) + 0] = values[i].X;
            data[(i * 4) + 1] = values[i].Y;
            data[(i * 4) + 2] = values[i].Z;
            data[(i * 4) + 3] = values[i].W;
        }

        buffer.WriteArray<float>(offset, data);
    }

    public void SetValue(ReadOnlySpan<Matrix> values)
    {
        if (Kind != ShaderParameterKind.BufferMember)
        {
            throw new InvalidOperationException(Name + " is not a value parameter");
        }

        int count = values.Length;
        Span<float> data = count <= 16
            ? stackalloc float[count * 16]
            : new float[count * 16];

        for (int i = 0; i < count; i++)
        {
            Matrix value = values[i];
            int j = i * 16;

            data[j + 0] = value.M11;
            data[j + 1] = value.M12;
            data[j + 2] = value.M13;
            data[j + 3] = value.M14;

            data[j + 4] = value.M21;
            data[j + 5] = value.M22;
            data[j + 6] = value.M23;
            data[j + 7] = value.M24;

            data[j + 8] = value.M31;
            data[j + 9] = value.M32;
            data[j + 10] = value.M33;
            data[j + 11] = value.M34;

            data[j + 12] = value.M41;
            data[j + 13] = value.M42;
            data[j + 14] = value.M43;
            data[j + 15] = value.M44;
        }

        buffer.WriteArray<float>(offset, data);
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