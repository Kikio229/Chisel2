using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.Utilities;

[StructLayout(LayoutKind.Sequential)]
public struct ModelVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Normal;
    [Vertex(2)] public Vector3 Tangent;
    [Vertex(3)] public Vector3 Binormal;
    [Vertex(4)] public Vector2 TexCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct SkinnedModelVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Normal;
    [Vertex(2)] public Vector3 Tangent;
    [Vertex(3)] public Vector3 Binormal;
    [Vertex(4)] public Vector2 TexCoord;
    [Vertex(5)] public Vector4 BoneIndices;
    [Vertex(6)] public Vector4 BoneWeights;
}
[StructLayout(LayoutKind.Sequential)]
public struct SimpleModelVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Normal;
    [Vertex(2)] public Vector4 Color;
    [Vertex(3)] public Vector2 TexCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct WorldVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Normal;
    [Vertex(2)] public Vector3 Tangent;
    [Vertex(3)] public Vector3 Binormal;
    [Vertex(4)] public Vector2 TexCoord;
    [Vertex(5)] public Vector2 LightmapCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct TerrainVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Normal;
    [Vertex(2)] public Vector4 Tangent; // xyz = tangent, w = handedness
    [Vertex(3)] public Vector2 TexCoord;
    [Vertex(4)] public float BlendFactor;
    [Vertex(5)] public Vector2 LightmapCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct SkyboxVertex
{
    [Vertex(0)] public Vector3 Position;
}
[StructLayout(LayoutKind.Sequential)]
public struct DecalVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector2 TexCoord;
    [Vertex(2)] public Vector4 Color;
}
[StructLayout(LayoutKind.Sequential)]
public struct UnlitVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector4 Color;
}