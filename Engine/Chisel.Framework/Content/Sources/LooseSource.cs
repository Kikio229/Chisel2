using System;
using System.IO;

namespace Chisel.Framework;

public class LooseSource : ISource
{
    private string _root;

    public LooseSource(string root)
    {
        _root = root;
    }

    public Stream Open(string relativePath)
    {
        string nativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return File.OpenRead(Path.Combine(_root, nativePath));
    }

    public bool Exists(string relativePath)
    {
        string nativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(_root, nativePath));
    }
}
