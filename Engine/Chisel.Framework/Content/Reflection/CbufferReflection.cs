using System;

namespace Chisel.Framework;

public struct CbufferReflection
{
    public string Name;
    public uint Slot;
    public int SizeInBytes;
    public MemberReflection[] Members;

    public CbufferReflection()
    {
        Name = string.Empty;
        Slot = 0;
        SizeInBytes = 0;
        Members = Array.Empty<MemberReflection>();
    }
}
