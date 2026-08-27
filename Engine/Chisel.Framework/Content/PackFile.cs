using System;
using System.Collections.Generic;

namespace Chisel.Framework;

public struct PackFile
{
    public List<PackEntry> Entries;
    public const uint Magic = 0x4350414B; // CPAK
    public const int FormatVersion = 1;

    public PackFile()
    {
        Entries = new List<PackEntry>();
    }
}
