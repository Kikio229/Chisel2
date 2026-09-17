using System;
using System.IO;

namespace Chisel.Framework;

public interface ILoader<T>
{
    string[] Extensions { get; }
    T Load(Stream stream);
}
