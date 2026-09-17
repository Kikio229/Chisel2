using System;

namespace Chisel.Framework;

[AttributeUsage(AttributeTargets.Field)]
public class VertexAttribute : Attribute
{
    public uint Location { get; }

    public VertexAttribute(uint location)
    {
        Location = location;
    }
}