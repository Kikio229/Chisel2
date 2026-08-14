using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public class ContentManager
{
    IGraphicsDevice device;
    IContentSource source;
    Dictionary<string, object> cache = new Dictionary<string, object>();
    Dictionary<Type, object> contentLoaders = new Dictionary<Type, object>();

    public ContentManager(IContentSource source, IGraphicsDevice device)
    {
        this.source = source;
        this.device = device;
    }
    public void RegisterLoader<T>(IContentLoader<T> loader)
    {
        contentLoaders[typeof(T)] = loader;
    }
    public T Load<T>(string relativePath)
    {
        relativePath = ContentPath.Normalize(relativePath);
        string cacheKey = typeof(T).FullName + "::" + relativePath;

        if (cache.TryGetValue(cacheKey, out object cached))
        {
            return (T)cached;
        }

        if (!contentLoaders.TryGetValue(typeof(T), out object loaderObj))
        {
            throw new InvalidOperationException("No content loader registered for " + typeof(T).Name);
        }

        IContentLoader<T> loader = (IContentLoader<T>)loaderObj;
        string resolvedPath = ResolvePath(relativePath, loader.Extensions);

        using Stream stream = source.Open(resolvedPath);
        T result = loader.Load(stream);
        cache[cacheKey] = result;
        return result;
    }
    string ResolvePath(string relativePath, string[] extensions)
    {
        if (extensions == null || extensions.Length == 0)
        {
            return relativePath;
        }

        foreach (string extension in extensions)
        {
            string candidate = relativePath + "." + extension;

            if (source.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("No content file found for '" + relativePath + "' with any of: " + string.Join(", ", extensions));
    }
}