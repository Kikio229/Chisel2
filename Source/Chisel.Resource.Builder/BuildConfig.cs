using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using System.IO;
namespace Chisel.Resource.Builder;
internal class BuildConfig
{
    public List<string> ContentDirectories { get; set; } = new List<string>();
    public List<string> UnpackedPaths { get; set; } = new List<string>();
    public string ParentConfigPath { get; set; }
}
class ResolvedBuildConfig
{
    public List<string> ContentDirectories { get; } = new List<string>();
    public List<string> UnpackedPaths { get; } = new List<string>();
}
internal class BuildOptions
{
    public bool Pack { get; set; } = false;
    public List<string> UnpackedPaths { get; set; } = new List<string>();
}
static class BuildConfigLoader
{
    public static ResolvedBuildConfig Resolve(string configPath)
    {
        ResolvedBuildConfig resolved = new ResolvedBuildConfig();
        Collect(configPath, resolved);
        return resolved;
    }

    static void Collect(string configPath, ResolvedBuildConfig resolved)
    {
        BuildConfig config = JsonSerializer.Deserialize<BuildConfig>(File.ReadAllText(configPath));
        string configDir = Path.GetDirectoryName(configPath);

        if (!string.IsNullOrEmpty(config.ParentConfigPath))
        {
            string parentPath = Path.GetFullPath(Path.Combine(configDir, config.ParentConfigPath));
            Collect(parentPath, resolved);
        }

        foreach (string relativeDir in config.ContentDirectories)
        {
            resolved.ContentDirectories.Add(Path.GetFullPath(Path.Combine(configDir, relativeDir)));
        }

        foreach (string unpackedPath in config.UnpackedPaths)
        {
            resolved.UnpackedPaths.Add(unpackedPath);
        }
    }
}