using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;

static class BuiltInShaderRegistry
{
    const string ResourcePrefix = "Chisel.Resource.Builder.ShaderLibrary.BuiltIn.";
    const string ResourceSuffix = ".hlsl";

    static string[] names;

    public static string[] Names
    {
        get
        {
            names ??= DiscoverNames();
            return names;
        }
    }

    static string[] DiscoverNames()
    {
        Assembly assembly = typeof(BuiltInShaderRegistry).Assembly;
        List<string> result = new List<string>();

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) &&
                resourceName.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                int start = ResourcePrefix.Length;
                int length = resourceName.Length - ResourcePrefix.Length - ResourceSuffix.Length;
                result.Add(resourceName.Substring(start, length));
            }
        }

        return result.ToArray();
    }

    public static string Resolve(string name)
    {
        string resourceName = "Chisel.Resource.Builder.ShaderLibrary.BuiltIn." + name + ".hlsl";
        Assembly assembly = typeof(BuiltInShaderRegistry).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new InvalidOperationException("No built-in shader named '" + name + "' is embedded in the builder.");
        }

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}