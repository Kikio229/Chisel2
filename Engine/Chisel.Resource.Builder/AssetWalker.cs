using Chisel.Resource.Builder;
using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Chisel.Resource.Builder;
internal class AssetWalker
{
    public static void Build(List<string> contentDirectories, string outputDirectory, BuildOptions options)
    {
        HashSet<string> expectedOutputs = new HashSet<string>();
        List<(string RelativePath, byte[] Data)> packedEntries = new List<(string, byte[])>();

        Console.WriteLine($"[Chisel Packer]: {(options.Pack ? "Packing is ENABLED. Files will be baked into Assets.CPK" : "Packing is DISABLED. Files will be copied over directly.")}");

        foreach (string directory in contentDirectories)
        {
            foreach (string sourceFile in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(directory, sourceFile);
                string extension = Path.GetExtension(sourceFile);
                AssetType type = AssetTypeMap.Resolve(extension);
                IAssetHandler handler = AssetHandlerRegistry.Resolve(type);

                byte[] data = handler.Convert(sourceFile); 
                string outputRelativePath = handler.OutputExtension != null
                    ? Path.ChangeExtension(relativePath, handler.OutputExtension)
                    : relativePath;

                outputRelativePath = ContentPath.Normalize(outputRelativePath);

                if (!options.Pack || IsUnpacked(relativePath, options.UnpackedPaths))
                {
                    string outputPath = Path.Combine(outputDirectory, outputRelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, data);
                    expectedOutputs.Add(outputRelativePath);
                    Console.WriteLine($"[Chisel Packer]: Copied {relativePath} to {outputRelativePath}");
                }
                else
                {
                    packedEntries.Add((outputRelativePath, data));
                    Console.WriteLine($"[Chisel Packer]: Packed {relativePath} to VFS::{outputRelativePath}");
                }
            }
        }
        ShaderAssetHandler shaderHandler = (ShaderAssetHandler)AssetHandlerRegistry.Resolve(AssetType.Shader);

        List<IBuiltInAssetSource> builtInSources = new List<IBuiltInAssetSource>
        {
            new BuiltInRegistry<ShaderAssetKind>(),
            new BuiltInRegistry<TextureAssetKind>(),
            new BuiltInRegistry<FontAssetKind>(),
        };

        foreach (IBuiltInAssetSource source in builtInSources)
        {
            foreach ((string relativePath, byte[] data, string label) in source.Discover())
            {
                if (!options.Pack || IsUnpacked(relativePath, options.UnpackedPaths))
                {
                    string outputPath = Path.Combine(outputDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    File.WriteAllBytes(outputPath, data);
                    expectedOutputs.Add(relativePath);
                    Console.WriteLine($"[Chisel Packer]: Compiled built-in {label} to VFS::{relativePath}");
                }
                else
                {
                    packedEntries.Add((relativePath, data));
                    Console.WriteLine($"[Chisel Packer]: Packed built-in {label} to VFS::{relativePath}");
                }
            }
        }

        if (options.Pack && packedEntries.Count > 0)
        {
            string pakPath = Path.Combine(outputDirectory, "assets.cpk");
            Directory.CreateDirectory(outputDirectory);
            PakWriter.Write(pakPath, packedEntries);
            expectedOutputs.Add("assets.cpk");
        }

        CleanOutputDirectory(outputDirectory, expectedOutputs);
    }

    static bool IsUnpacked(string relativePath, List<string> unpackedPaths)
    {
        foreach (string prefix in unpackedPaths)
        {
            string normalizedPrefix = prefix.TrimEnd('/', '\\');

            if (relativePath == normalizedPrefix || relativePath.StartsWith(normalizedPrefix + Path.DirectorySeparatorChar))
            {
                return true;
            }
        }

        return false;
    }
    static void CleanOutputDirectory(string outputDirectory, HashSet<string> expectedOutputs)
    {
        if (!Directory.Exists(outputDirectory))
        {
            return;
        }

        foreach (string existingFile in Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = ContentPath.Normalize(Path.GetRelativePath(outputDirectory, existingFile));

            if (!expectedOutputs.Contains(relativePath))
            {
                File.Delete(existingFile);
            }
        }

        RemoveEmptyDirectories(outputDirectory);
    }

    static void RemoveEmptyDirectories(string directory)
    {
        foreach (string subDirectory in Directory.EnumerateDirectories(directory))
        {
            RemoveEmptyDirectories(subDirectory);

            if (!Directory.EnumerateFileSystemEntries(subDirectory).Any())
            {
                Directory.Delete(subDirectory);
            }
        }
    }
}
