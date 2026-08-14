using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using Chisel.Framework;

namespace Chisel.Framework.ImGUI;

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector2 UV;
    [VertexAttribute(2)] public Vector4 Color;
}