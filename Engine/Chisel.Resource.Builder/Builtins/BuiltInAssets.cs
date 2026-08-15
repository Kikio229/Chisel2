using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;
public interface IBuiltInAssetSource
{
    IEnumerable<(string RelativePath, byte[] Data, string Label)> Discover();
}

public interface IBuiltInAssetKind
{
    string Label { get; }
    string ResourcePrefix { get; }
    string ResourceSuffix { get; }
    byte[] Resolve(string name, byte[] embeddedData);
    string GetOutputPath(string name);
}
public class BuiltInRegistry<T> : IBuiltInAssetSource where T : IBuiltInAssetKind, new()
{
    static string[] names;
    static readonly T kind = new T();

    public string[] Names
    {
        get
        {
            names ??= DiscoverNames();
            return names;
        }
    }

    public IEnumerable<(string RelativePath, byte[] Data, string Label)> Discover()
    {
        foreach (string name in Names)
        {
            string resourceName = kind.ResourcePrefix + name + kind.ResourceSuffix;
            using Stream stream = typeof(T).Assembly.GetManifestResourceStream(resourceName);
            using MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            byte[] embeddedData = memoryStream.ToArray();

            byte[] data = kind.Resolve(name, embeddedData);
            yield return (kind.GetOutputPath(name), data, kind.Label);
        }
    }

    static string[] DiscoverNames()
    {
        List<string> result = new List<string>();

        foreach (string resourceName in typeof(T).Assembly.GetManifestResourceNames())
        {
            if (resourceName.StartsWith(kind.ResourcePrefix, StringComparison.Ordinal) &&
                resourceName.EndsWith(kind.ResourceSuffix, StringComparison.Ordinal))
            {
                int start = kind.ResourcePrefix.Length;
                int length = resourceName.Length - kind.ResourcePrefix.Length - kind.ResourceSuffix.Length;
                result.Add(resourceName.Substring(start, length));
            }
        }

        return result.ToArray();
    }
}