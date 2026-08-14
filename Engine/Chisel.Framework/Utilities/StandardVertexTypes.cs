using Microsoft.Xna.Framework;
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
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector3 Normal;
    [VertexAttribute(2)] public Vector3 Tangent;
    [VertexAttribute(3)] public Vector3 Binormal;
    [VertexAttribute(4)] public Vector2 TexCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct SkinnedModelVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector3 Normal;
    [VertexAttribute(2)] public Vector3 Tangent;
    [VertexAttribute(3)] public Vector3 Binormal;
    [VertexAttribute(4)] public Vector2 TexCoord;
    [VertexAttribute(5)] public Vector4 BoneIndices;
    [VertexAttribute(6)] public Vector4 BoneWeights;
}
[StructLayout(LayoutKind.Sequential)]
public struct SimpleModelVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector3 Normal;
    [VertexAttribute(2)] public Vector4 Color;
    [VertexAttribute(3)] public Vector2 TexCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct WorldVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector3 Normal;
    [VertexAttribute(2)] public Vector3 Tangent;
    [VertexAttribute(3)] public Vector3 Binormal;
    [VertexAttribute(4)] public Vector2 TexCoord;
    [VertexAttribute(5)] public Vector2 LightmapCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct TerrainVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector3 Normal;
    [VertexAttribute(2)] public Vector4 Tangent; // xyz = tangent, w = handedness
    [VertexAttribute(3)] public Vector2 TexCoord;
    [VertexAttribute(4)] public float BlendFactor;
    [VertexAttribute(5)] public Vector2 LightmapCoord;
}
[StructLayout(LayoutKind.Sequential)]
public struct SkyboxVertex
{
    [VertexAttribute(0)] public Vector3 Position;
}
[StructLayout(LayoutKind.Sequential)]
public struct DecalVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector2 TexCoord;
    [VertexAttribute(2)] public Vector4 Color;
}
[StructLayout(LayoutKind.Sequential)]
public struct UnlitVertex
{
    [VertexAttribute(0)] public Vector3 Position;
    [VertexAttribute(1)] public Vector4 Color;
}