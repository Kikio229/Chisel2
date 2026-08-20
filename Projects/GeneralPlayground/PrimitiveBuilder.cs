using Chisel.Framework;
using Chisel.Framework.Utilities;
using System;
using System.Collections.Generic;

public struct MeshData
{
    public ModelVertex[] Vertices;
    public uint[] Indices;
}

public static class PrimitiveBuilder
{
    public static MeshData CreateCube(float size = 1f, Vector2 uvTiling = default)
    {
        Vector2 tile = uvTiling == default ? Vector2.One : uvTiling;

        Vector3 nx = new(1, 0, 0), nnx = new(-1, 0, 0);
        Vector3 ny = new(0, 1, 0), nny = new(0, -1, 0);
        Vector3 nz = new(0, 0, 1), nnz = new(0, 0, -1);

        Vector2 uv00 = new(0, 0), uv10 = new(tile.X, 0), uv11 = new(tile.X, tile.Y), uv01 = new(0, tile.Y);

        float h = size * 0.5f;

        var verts = new ModelVertex[]
        {
            // +X
            new() { Position = new(h,-h,-h), Normal = nx, TexCoord = uv00 },
            new() { Position = new(h, h,-h), Normal = nx, TexCoord = uv10 },
            new() { Position = new(h, h, h), Normal = nx, TexCoord = uv11 },
            new() { Position = new(h,-h, h), Normal = nx, TexCoord = uv01 },

            // -X
            new() { Position = new(-h,-h, h), Normal = nnx, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = nnx, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnx, TexCoord = uv11 },
            new() { Position = new(-h,-h,-h), Normal = nnx, TexCoord = uv01 },

            // +Y
            new() { Position = new(-h, h,-h), Normal = ny, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = ny, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = ny, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = ny, TexCoord = uv01 },

            // -Y
            new() { Position = new(-h,-h, h), Normal = nny, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nny, TexCoord = uv10 },
            new() { Position = new( h,-h,-h), Normal = nny, TexCoord = uv11 },
            new() { Position = new( h,-h, h), Normal = nny, TexCoord = uv01 },

            // +Z
            new() { Position = new(-h,-h, h), Normal = nz, TexCoord = uv00 },
            new() { Position = new( h,-h, h), Normal = nz, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = nz, TexCoord = uv11 },
            new() { Position = new(-h, h, h), Normal = nz, TexCoord = uv01 },

            // -Z
            new() { Position = new( h,-h,-h), Normal = nnz, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nnz, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnz, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = nnz, TexCoord = uv01 },
        };

        var indices = new uint[6 * 6];
        for (uint face = 0; face < 6; face++)
        {
            uint b = face * 4;
            uint o = face * 6;
            indices[o + 0] = b + 0; indices[o + 1] = b + 1; indices[o + 2] = b + 2;
            indices[o + 3] = b + 2; indices[o + 4] = b + 3; indices[o + 5] = b + 0;
        }

        GenerateTangents(verts, indices);
        return new MeshData { Vertices = verts, Indices = indices };
    }

    public static MeshData CreatePlane(float width = 1f, float depth = 1f, Vector2 uvTiling = default)
    {
        Vector2 tile = uvTiling == default ? Vector2.One : uvTiling;

        float hw = width * 0.5f;
        float hd = depth * 0.5f;

        var verts = new ModelVertex[]
        {
            new() { Position = new(-hw, 0,-hd), Normal = Vector3.UnitY, TexCoord = new(0, 0) },
            new() { Position = new(-hw, 0, hd), Normal = Vector3.UnitY, TexCoord = new(0, tile.Y) },
            new() { Position = new( hw, 0, hd), Normal = Vector3.UnitY, TexCoord = new(tile.X, tile.Y) },
            new() { Position = new( hw, 0,-hd), Normal = Vector3.UnitY, TexCoord = new(tile.X, 0) },
        };

        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };

        GenerateTangents(verts, indices);
        return new MeshData { Vertices = verts, Indices = indices };
    }

    public static MeshData CreateSphere(float radius = 0.5f, int slices = 24, int stacks = 16)
    {
        var verts = new List<ModelVertex>((slices + 1) * (stacks + 1));

        for (int stack = 0; stack <= stacks; stack++)
        {
            float v = (float)stack / stacks;
            float phi = v * MathF.PI; // 0 = top pole, PI = bottom pole
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                float u = (float)slice / slices;
                float theta = u * MathF.PI * 2f;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                Vector3 n = new(sinPhi * cosTheta, cosPhi, sinPhi * sinTheta);

                verts.Add(new ModelVertex
                {
                    Position = n * radius,
                    Normal = n,
                    TexCoord = new Vector2(u, v)
                });
            }
        }

        var indices = new List<uint>(slices * stacks * 6);
        int ringVerts = slices + 1;
        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                uint a = (uint)(stack * ringVerts + slice);
                uint b = a + (uint)ringVerts;
                uint c = a + 1;
                uint d = b + 1;

                indices.Add(a); indices.Add(c); indices.Add(b);
                indices.Add(c); indices.Add(d); indices.Add(b);
            }
        }

        var vertArray = verts.ToArray();
        var indexArray = indices.ToArray();
        GenerateTangents(vertArray, indexArray);
        return new MeshData { Vertices = vertArray, Indices = indexArray };
    }

    /// <summary>
    /// Derives per-vertex tangent/binormal from position + UV gradients across
    /// each triangle (accumulated, then Gram-Schmidt orthogonalized against the
    /// vertex normal). One shared implementation for all three primitives rather
    /// than hand-deriving per-face directions three separate times.
    ///
    /// Sign/handedness of the result matters once real normal maps are authored;
    /// with the flat default normal map this is purely there so Model.hlsl has
    /// *something* orthonormal to build its TBN basis from.
    /// </summary>
    static void GenerateTangents(ModelVertex[] verts, uint[] indices)
    {
        var tan = new Vector3[verts.Length];
        var bitan = new Vector3[verts.Length];

        for (int i = 0; i < indices.Length; i += 3)
        {
            uint i0 = indices[i], i1 = indices[i + 1], i2 = indices[i + 2];

            Vector3 p0 = verts[i0].Position, p1 = verts[i1].Position, p2 = verts[i2].Position;
            Vector2 uv0 = verts[i0].TexCoord, uv1 = verts[i1].TexCoord, uv2 = verts[i2].TexCoord;

            Vector3 edge1 = p1 - p0;
            Vector3 edge2 = p2 - p0;
            Vector2 duv1 = uv1 - uv0;
            Vector2 duv2 = uv2 - uv0;

            float denom = duv1.X * duv2.Y - duv2.X * duv1.Y;
            float f = MathF.Abs(denom) < 1e-8f ? 0f : 1f / denom;

            Vector3 tangent = (edge1 * duv2.Y - edge2 * duv1.Y) * f;
            Vector3 bitangent = (edge2 * duv1.X - edge1 * duv2.X) * f;

            tan[i0] += tangent; tan[i1] += tangent; tan[i2] += tangent;
            bitan[i0] += bitangent; bitan[i1] += bitangent; bitan[i2] += bitangent;
        }

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = verts[i].Normal;
            Vector3 t = tan[i] - n * n.DotProduct(tan[i]);

            t = t.LengthSquared() > 1e-12f ? t.Normalize() : ArbitraryTangent(n);

            Vector3 b = n.CrossProduct(t);
            if (b.DotProduct(bitan[i]) < 0f)
                b = b.Negate();

            verts[i].Tangent = t;
            verts[i].Binormal = b;
        }
    }

    static Vector3 ArbitraryTangent(Vector3 n)
    {
        Vector3 up = MathF.Abs(n.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        return up.CrossProduct(n).Normalize();
    }
}

public class MeshBuffers : IDisposable
{
    public VertexBuffer<ModelVertex> Vertices { get; }
    public IndexBuffer Indices { get; }
    public int IndexCount => Indices.Count;

    public MeshBuffers(IGraphicsDevice device, MeshData data)
    {
        Vertices = new VertexBuffer<ModelVertex>(device, data.Vertices.Length);
        Vertices.SetData(data.Vertices);

        Indices = new IndexBuffer(device, data.Indices.Length);
        Indices.SetData(data.Indices);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
    }
}