using System;
using System.IO;

namespace Chisel.Framework;

public class GenericLoader : ILoader<string>
{
    public string[] Extensions { get; }

    public GenericLoader()
    {
        Extensions = Array.Empty<string>();
    }

    public string Load(Stream stream)
    {
        using StreamReader reader = new StreamReader(stream);
        string text = reader.ReadToEnd();
        return text;
    }
}
