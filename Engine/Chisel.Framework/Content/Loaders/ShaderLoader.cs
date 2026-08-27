using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Framework;

public class ShaderLoader : ILoader<Dictionary<string, IShader[]>>
{
    public string[] Extensions { get; }
    public const string FileExtension = "csl";

    private IGraphicsDevice _device;

    public ShaderLoader(IGraphicsDevice device)
    {
        Extensions = new string[1] 
        {
            FileExtension 
        };

        _device = device;
    }

    public Dictionary<string, IShader[]> Load(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);

        int variantCount = reader.ReadInt32();
        List<ShaderEntry> matched = new List<ShaderEntry>();

        for (int i = 0; i < variantCount; i++)
        {
            ShaderEntry variant = new ShaderEntry
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

            if (variant.Backend == _device.Backend)
            {
                matched.Add(variant);
            }
        }

        if (matched.Count == 0)
        {
            throw new InvalidOperationException("Shader has no compiled variants for " + _device.Backend);
        }

        long blobStart = stream.Position;
        Dictionary<string, List<IShader>> techniques = new Dictionary<string, List<IShader>>();

        foreach (ShaderEntry variant in matched)
        {
            stream.Position = blobStart + variant.BytecodeOffset;
            byte[] bytecode = reader.ReadBytes(variant.BytecodeLength);

            stream.Position = blobStart + variant.ReflectionOffset;
            ShaderReflection reflection = ShaderReflectionSerializer.Read(reader);

            IShader shader = _device.CreateShader(new ShaderDescription
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