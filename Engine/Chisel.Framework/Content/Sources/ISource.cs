using System;
using System.IO;

namespace Chisel.Framework;

public interface ISource
{
    bool Exists(string relativePath);
    Stream Open(string relativePath);
}
