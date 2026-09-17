using System;

namespace Chisel.Framework;

public struct VertexLayoutDescription
{
    public int Stride;
    public VertexAttributeDescription[] Attributes;

    public VertexLayoutDescription()
    {
        Stride = 0; 
        Attributes = new VertexAttributeDescription[0];
    }
}