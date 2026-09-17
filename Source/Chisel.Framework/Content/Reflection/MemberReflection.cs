using System;

namespace Chisel.Framework;

public struct MemberReflection
{
    public string Name;
    public int Offset;
    public int SizeInBytes;

    public MemberReflection()
    {
        Name = string.Empty;
        Offset = 0;
        SizeInBytes = 0;
    }
}