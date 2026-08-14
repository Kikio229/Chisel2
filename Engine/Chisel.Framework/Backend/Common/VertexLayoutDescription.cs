using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public enum VertexElementFormat
{
    Float1,
    Float2,
    Float3,
    Float4,
    Int1,
    UInt1,
    Byte1,
}
public struct VertexAttributeDescription
{
    public uint Location;
    public VertexElementFormat Format;
    public int Offset;
}
public struct VertexLayoutDescription
{
    public VertexAttributeDescription[] Attributes;
    public int Stride;
}