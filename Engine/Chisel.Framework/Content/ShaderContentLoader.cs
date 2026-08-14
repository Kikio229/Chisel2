using Chisel.Framework;
using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Framework;
public class ShaderLoader : IContentLoader<Dictionary<string, IShader[]>>
{
    public string[] Extensions => new[] { ShaderContentInfo.FileExtension };

    IGraphicsDevice device;

    public ShaderLoader(IGraphicsDevice device)
    {
        this.device = device;
    }

    public Dictionary<string, IShader[]> Load(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);

        int variantCount = reader.ReadInt32();
        List<ShaderVariantEntry> matched = new List<ShaderVariantEntry>();

        for (int i = 0; i < variantCount; i++)
        {
            ShaderVariantEntry variant = new ShaderVariantEntry
            {
                Technique = reader.ReadString(),
                Backend = (GraphicsBackend)reader.ReadInt32(),
                Stage = (ShaderStage)reader.ReadInt32(),
                Entry = reader.ReadString(),
                BytecodeOffset = reader.ReadInt32(),
                BytecodeLength = reader.ReadInt32(),
                ReflectionOffset = reader.ReadInt32(),
                ReflectionLength = reader.ReadInt32(),
            };

            if (variant.Backend == device.Backend)
            {
                matched.Add(variant);
            }
        }

        if (matched.Count == 0)
        {
            throw new InvalidOperationException("Shader has no compiled variants for " + device.Backend);
        }

        long blobStart = stream.Position;
        Dictionary<string, List<IShader>> techniques = new Dictionary<string, List<IShader>>();

        foreach (ShaderVariantEntry variant in matched)
        {
            stream.Position = blobStart + variant.BytecodeOffset;
            byte[] bytecode = reader.ReadBytes(variant.BytecodeLength);

            stream.Position = blobStart + variant.ReflectionOffset;
            ShaderReflection reflection = ShaderReflectionSerializer.Read(reader);

            IShader shader = device.CreateShader(new ShaderDescription
            {
                Entry = variant.Entry,
                Stage = variant.Stage,
                Bytecode = bytecode,
                Reflection = reflection,
            });

            if (!techniques.TryGetValue(variant.Technique, out List<IShader> stages))
            {
                stages = new List<IShader>();
                techniques[variant.Technique] = stages;
            }

            stages.Add(shader);
        }

        Dictionary<string, IShader[]> result = new Dictionary<string, IShader[]>();

        foreach (KeyValuePair<string, List<IShader>> pair in techniques)
        {
            result[pair.Key] = pair.Value.ToArray();
        }

        return result;
    }
}

public class ShaderPassLoader : IContentLoader<ShaderPass>
{
    public string[] Extensions => new[] { ShaderContentInfo.FileExtension };

    IGraphicsDevice device;
    ShaderLoader shaderLoader;

    public ShaderPassLoader(IGraphicsDevice device)
    {
        this.device = device;
        shaderLoader = new ShaderLoader(device);
    }

    public ShaderPass Load(Stream stream)
    {
        Dictionary<string, IShader[]> techniques = shaderLoader.Load(stream);

        if (!techniques.TryGetValue("Default", out IShader[] stages))
        {
            throw new InvalidOperationException(
                "This shader declares named techniques (" + string.Join(", ", techniques.Keys) +
                ") but no 'Default' — load it as a ShaderEffect instead.");
        }

        return new ShaderPass(device, stages);
    }
}

public class ShaderEffectLoader : IContentLoader<ShaderEffect>
{
    public string[] Extensions => new[] { ShaderContentInfo.FileExtension };

    IGraphicsDevice device;
    ShaderLoader shaderLoader;

    public ShaderEffectLoader(IGraphicsDevice device)
    {
        this.device = device;
        shaderLoader = new ShaderLoader(device);
    }

    public ShaderEffect Load(Stream stream)
    {
        Dictionary<string, IShader[]> techniques = shaderLoader.Load(stream);
        ShaderEffect effect = new ShaderEffect();

        foreach (KeyValuePair<string, IShader[]> pair in techniques)
        {
            ShaderPass program = new ShaderPass(device, pair.Value);
            effect.AddTechnique(pair.Key, program);
        }

        return effect;
    }
}