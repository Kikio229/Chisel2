using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Framework;

public class ShaderPassLoader : ILoader<ShaderPass>
{
    public string[] Extensions { get; }

    private ShaderLoader _loader;
    private IGraphicsDevice _device;

    public ShaderPassLoader(IGraphicsDevice device)
    {
        Extensions = new string[1]
        {
            ShaderLoader.FileExtension
        };

        _loader = new ShaderLoader(device);
        _device = device;
    }

    public ShaderPass Load(Stream stream)
    {
        Dictionary<string, IShader[]> techniques = _loader.Load(stream);

        if (!techniques.TryGetValue("Default", out IShader[]? stages))
        {
            throw new InvalidOperationException("This shader declares named techniques (" + string.Join(", ", techniques.Keys) +
                ") but no 'Default' — load it as a ShaderEffect instead.");
        }

        return new ShaderPass(_device, stages);
    }
}
