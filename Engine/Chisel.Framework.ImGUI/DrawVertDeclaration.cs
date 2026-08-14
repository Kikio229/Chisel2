using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using Chisel.Framework;

namespace Chisel.Framework.ImGUI;

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector2 UV;
    [Vertex(2)] public Vector4 Color;
}