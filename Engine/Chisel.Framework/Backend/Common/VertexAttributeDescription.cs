using System;

namespace Chisel.Framework;

public struct VertexAttributeDescription
{
    public int Offset;
    public uint Location;
    public VertexFormat Format;

    public VertexAttributeDescription()
    {
        Offset = 0;
        Location = 0;
        Format = VertexFormat.Float1;
    }
}