using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Framework;

public class ShaderEffectLoader : ILoader<ShaderEffect>
{
    public string[] Extensions { get; }

    private ShaderLoader _loader;
    private IGraphicsDevice _device;

    public ShaderEffectLoader(IGraphicsDevice device)
    {
        Extensions = new string[1]
        {
            ShaderLoader.FileExtension
        };

        _loader = new ShaderLoader(device);
        _device = device;
    }

    public ShaderEffect Load(Stream stream)
    {
        Dictionary<string, IShader[]> techniques = _loader.Load(stream);
        ShaderEffect effect = new ShaderEffect();

        foreach (KeyValuePair<string, IShader[]> t in techniques)
        {
            ShaderPass program = new ShaderPass(_device, t.Value);
            effect.AddTechnique(t.Key, program);
        }

        return effect;
    }
}