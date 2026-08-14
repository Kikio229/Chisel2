using Chisel.Resource.Builder;
using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Chisel.Resource.Builder;

class ShaderAssetHandler : IAssetHandler
{
    public string OutputExtension => ShaderContentInfo.FileExtension;

    IShaderCompiler[] compilers;

    static readonly (string Entry, ShaderStage Stage)[] KnownEntryPoints = new (string, ShaderStage)[]
    {
        ("VSMain", ShaderStage.Vertex),
        ("PSMain", ShaderStage.Pixel),
        ("CSMain", ShaderStage.Compute),
    };

    // for #technique
    static readonly Regex TechniqueDirective = new Regex(
        @"^\s*#technique\s+(\S+)(?:\s+vertex=(\S+))?(?:\s+pixel=(\S+))?(?:\s+compute=(\S+))?\s*$",
        RegexOptions.Multiline);

    public ShaderAssetHandler(params IShaderCompiler[] compilers)
    {
        this.compilers = compilers;
    }
    public static void DumpPreprocessedIfRequested(string name, string source)
    {
        if (Environment.GetEnvironmentVariable("CHISEL_DUMP_PREPROCESSED") == null)
        {
            return;
        }

        Console.WriteLine("Dumping");

        string directory = Path.Combine(AppContext.BaseDirectory, "PreprocessedDebug");
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, name + ".preprocessed.hlsl");
        File.WriteAllText(path, source);
    }
    public byte[] Convert(string sourcePath)
    {
        string source = File.ReadAllText(sourcePath);
        source = ShaderPreprocessor.ExpandLibraries(source);
        DumpPreprocessedIfRequested(Path.GetFileNameWithoutExtension(sourcePath), source);
        return CompileSource(source);
    }

    public byte[] CompileSource(string source)
    {
        List<(string Technique, string Entry, ShaderStage Stage)> entryPoints = DiscoverEntryPoints(source);

        if (entryPoints.Count == 0)
        {
            throw new InvalidOperationException("No recognized entry points or #technique directives found.");
        }
        string compileSource = StripTechniqueDirectives(source);

        List<ShaderVariantEntry> headers = new List<ShaderVariantEntry>();
        List<byte[]> bytecodeBlobs = new List<byte[]>();
        List<byte[]> reflectionBlobs = new List<byte[]>();

        foreach ((string technique, string entry, ShaderStage stage) in entryPoints)
        {
            foreach (IShaderCompiler compiler in compilers)
            {
                (byte[] bytecode, ShaderReflection reflection) = compiler.Compile(compileSource, entry, stage);

                using MemoryStream reflectionStream = new MemoryStream();
                using BinaryWriter reflectionWriter = new BinaryWriter(reflectionStream);
                ShaderReflectionSerializer.Write(reflectionWriter, reflection);

                headers.Add(new ShaderVariantEntry
                {
                    Technique = technique,
                    Backend = compiler.Backend,
                    Stage = stage,
                    Entry = entry,
                });

                bytecodeBlobs.Add(bytecode);
                reflectionBlobs.Add(reflectionStream.ToArray());
            }
        }

        int[] bytecodeOffsets = new int[headers.Count];
        int[] reflectionOffsets = new int[headers.Count];
        int offset = 0;

        for (int i = 0; i < headers.Count; i++)
        {
            bytecodeOffsets[i] = offset;
            offset += bytecodeBlobs[i].Length;
            reflectionOffsets[i] = offset;
            offset += reflectionBlobs[i].Length;
        }

        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(headers.Count);

        for (int i = 0; i < headers.Count; i++)
        {
            writer.Write(headers[i].Technique);
            writer.Write((int)headers[i].Backend);
            writer.Write((int)headers[i].Stage);
            writer.Write(headers[i].Entry);
            writer.Write(bytecodeOffsets[i]);
            writer.Write(bytecodeBlobs[i].Length);
            writer.Write(reflectionOffsets[i]);
            writer.Write(reflectionBlobs[i].Length);
        }

        for (int i = 0; i < headers.Count; i++)
        {
            writer.Write(bytecodeBlobs[i]);
            writer.Write(reflectionBlobs[i]);
        }

        return stream.ToArray();
    }
    static List<(string Technique, string Entry, ShaderStage Stage)> DiscoverEntryPoints(string source)
    {
        List<(string, string, ShaderStage)> result = new List<(string, string, ShaderStage)>();
        MatchCollection matches = TechniqueDirective.Matches(source);

        if (matches.Count == 0)
        {
            foreach ((string entry, ShaderStage stage) in KnownEntryPoints)
            {
                if (Regex.IsMatch(source, @"\b" + entry + @"\b"))
                {
                    result.Add(("Default", entry, stage));
                }
            }

            return result;
        }

        foreach (Match match in matches)
        {
            string technique = match.Groups[1].Value;

            if (match.Groups[2].Success)
            {
                result.Add((technique, match.Groups[2].Value, ShaderStage.Vertex));
            }
            if (match.Groups[3].Success)
            {
                result.Add((technique, match.Groups[3].Value, ShaderStage.Pixel));
            }
            if (match.Groups[4].Success)
            {
                result.Add((technique, match.Groups[4].Value, ShaderStage.Compute));
            }
        }

        return result;
    }
    static string StripTechniqueDirectives(string source)
    {
        return TechniqueDirective.Replace(source, string.Empty);
    }
}