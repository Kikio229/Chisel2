using System;
using System.Collections.Generic;
using System.IO;

namespace Chisel.Framework;

public class ContentManager
{
    private ISource _source;
    private Dictionary<string, object> _cache;
    private Dictionary<Type, object> _loaders;

    public ContentManager(ISource source)
    {
        _source = source;
        _cache = new Dictionary<string, object>();
        _loaders = new Dictionary<Type, object>();
    }

    public T Load<T>(string relativePath)
    {
        relativePath = ContentPath.Normalize(relativePath);
        string cacheKey = typeof(T).FullName + "::" + relativePath;

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return (T)cached;
        }

        if (!_loaders.TryGetValue(typeof(T), out object? loaderObj))
        {
            throw new InvalidOperationException("No content loader registered for " + typeof(T).Name);
        }

        ILoader<T> loader = (ILoader<T>)loaderObj;
        string resolvedPath = ResolvePath(relativePath, loader.Extensions);

        using Stream stream = _source.Open(resolvedPath);
        T result = loader.Load(stream);
        _cache[cacheKey] = result!;

        return result;
    }

    public byte[] LoadBytes(string relativePath)
    {
        relativePath = ContentPath.Normalize(relativePath);
        string cacheKey = "byte[]::" + relativePath;

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return (byte[])cached;
        }

        using Stream stream = _source.Open(relativePath);
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        byte[] result = memoryStream.ToArray();

        _cache[cacheKey] = result;
        return result;
    }

    public void RegisterLoader<T>(ILoader<T> loader)
    {
        _loaders[typeof(T)] = loader;
    }

    private string ResolvePath(string relativePath, string[] extensions)
    {
        if (extensions == null || extensions.Length == 0)
        {
            return relativePath;
        }

        foreach (string e in extensions)
        {
            string candidate = relativePath + "." + e;

            if (_source.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("No content file found for '" + relativePath + "' with any of: " + string.Join(", ", extensions));
    }
}