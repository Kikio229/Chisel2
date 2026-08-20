using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class ShaderEffectParameterCollection
{
    ShaderEffect effect;
    Dictionary<string, ShaderEffectParameter> cache = new Dictionary<string, ShaderEffectParameter>();

    internal ShaderEffectParameterCollection(ShaderEffect effect)
    {
        this.effect = effect;
    }

    public ShaderEffectParameter this[string name]
    {
        get
        {
            if (cache.TryGetValue(name, out ShaderEffectParameter cached))
            {
                return cached;
            }

            bool existsAnywhere = false;

            foreach (ShaderPass program in effect.AllPrograms)
            {
                if (program.Parameters[name] != null)
                {
                    existsAnywhere = true;
                    break;
                }
            }

            if (!existsAnywhere)
            {
                return null;
            }

            ShaderEffectParameter parameter = new ShaderEffectParameter(name, effect);
            cache[name] = parameter;
            return parameter;
        }
    }
}
public class ShaderEffectParameter
{
    string name;
    ShaderEffect effect;

    internal ShaderEffectParameter(string name, ShaderEffect effect)
    {
        this.name = name;
        this.effect = effect;
    }

    public void SetValue<T>(in T value) where T : unmanaged
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(value);
        }
    }

    public void SetValue(Matrix value)
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(value);
        }
    }

    public void SetValue(Texture2D texture)
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(texture.Image);
        }
    }

    public void SetValue<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(values);
        }
    }

    public void SetValue(IImage image)
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(image);
        }
    }

    public void SetValue(ISampler sampler)
    {
        foreach (ShaderPass program in effect.AllPrograms)
        {
            program.Parameters[name]?.SetValue(sampler);
        }
    }
}