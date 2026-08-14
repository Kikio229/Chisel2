using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;

static class ShaderLibraryRegistry
{
    const string ResourcePrefix = "Chisel.Resource.Builder.ShaderLibrary.";
    const string ResourceSuffix = ".hlsli";

    static Dictionary<string, string> resourcesByName;
    static Dictionary<string, string> cache = new Dictionary<string, string>();

    public static string Resolve(string name)
    {
        if (cache.TryGetValue(name, out string cached))
        {
            return cached;
        }

        EnsureIndex();

        if (!resourcesByName.TryGetValue(name, out string resourceName))
        {
            string available = string.Join(", ", resourcesByName.Keys);
            throw new InvalidOperationException("No shader library named '" + name + "' is embedded in the builder. Available: " + available);
        }

        Assembly assembly = typeof(ShaderLibraryRegistry).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new StreamReader(stream);
        string text = reader.ReadToEnd();

        cache[name] = text;
        return text;
    }

    static void EnsureIndex()
    {
        if (resourcesByName != null)
        {
            return;
        }

        resourcesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(ShaderLibraryRegistry).Assembly;

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) &&
                resourceName.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                string libraryName = resourceName.Substring(ResourcePrefix.Length, resourceName.Length - ResourcePrefix.Length - ResourceSuffix.Length);
                resourcesByName[libraryName] = resourceName;
            }
        }
    }
}