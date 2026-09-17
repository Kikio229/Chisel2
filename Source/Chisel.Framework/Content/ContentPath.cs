using System;

namespace Chisel.Framework;

public static class ContentPath
{
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }
}