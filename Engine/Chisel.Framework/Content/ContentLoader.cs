using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public interface IContentLoader<T>
{
    string[] Extensions { get; }
    T Load(Stream stream);
}
// TODO: texture,shader,etc loaders.


// This is a generic loader for anything else. JSON data, etc.
public class FileLoader : IContentLoader<string>
{
    public string[] Extensions => Array.Empty<string>();

    public string Load(Stream stream)
    {
        using StreamReader reader = new StreamReader(stream);
        string text = reader.ReadToEnd();
        return text;
    }
}