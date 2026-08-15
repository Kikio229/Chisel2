using Chisel.Framework;
using Chisel.Framework.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public struct MeshData
{
    public SimpleModelVertex[] Vertices;
    public uint[] Indices;
}

public static class PrimitiveBuilder
{
    public static MeshData CreateCube(float size = 1f, Vector4? color = null, Vector2 uvTiling = default)
    {
        Vector4 tint = color ?? Vector4.Zero;
        Vector2 tile = uvTiling == default ? Vector2.One : uvTiling;

        Vector3 nx = new(1, 0, 0), nnx = new(-1, 0, 0);
        Vector3 ny = new(0, 1, 0), nny = new(0, -1, 0);
        Vector3 nz = new(0, 0, 1), nnz = new(0, 0, -1);

        Vector2 uv00 = new(0, 0), uv10 = new(tile.X, 0), uv11 = new(tile.X, tile.Y), uv01 = new(0, tile.Y);

        float h = size * 0.5f;

        var verts = new SimpleModelVertex[]
        {
            // +X
            new() { Position = new(h,-h,-h), Normal = nx, Color = tint, TexCoord = uv00 },
            new() { Position = new(h, h,-h), Normal = nx, Color = tint, TexCoord = uv10 },
            new() { Position = new(h, h, h), Normal = nx, Color = tint, TexCoord = uv11 },
            new() { Position = new(h,-h, h), Normal = nx, Color = tint, TexCoord = uv01 },

            // -X
            new() { Position = new(-h,-h, h), Normal = nnx, Color = tint, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = nnx, Color = tint, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnx, Color = tint, TexCoord = uv11 },
            new() { Position = new(-h,-h,-h), Normal = nnx, Color = tint, TexCoord = uv01 },

            // +Y
            new() { Position = new(-h, h,-h), Normal = ny, Color = tint, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = ny, Color = tint, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = ny, Color = tint, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = ny, Color = tint, TexCoord = uv01 },

            // -Y
            new() { Position = new(-h,-h, h), Normal = nny, Color = tint, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nny, Color = tint, TexCoord = uv10 },
            new() { Position = new( h,-h,-h), Normal = nny, Color = tint, TexCoord = uv11 },
            new() { Position = new( h,-h, h), Normal = nny, Color = tint, TexCoord = uv01 },

            // +Z
            new() { Position = new(-h,-h, h), Normal = nz, Color = tint, TexCoord = uv00 },
            new() { Position = new( h,-h, h), Normal = nz, Color = tint, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = nz, Color = tint, TexCoord = uv11 },
            new() { Position = new(-h, h, h), Normal = nz, Color = tint, TexCoord = uv01 },

            // -Z
            new() { Position = new( h,-h,-h), Normal = nnz, Color = tint, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nnz, Color = tint, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnz, Color = tint, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = nnz, Color = tint, TexCoord = uv01 },
        };

        var indices = new uint[6 * 6];
        for (uint face = 0; face < 6; face++)
        {
            uint b = face * 4;
            uint o = face * 6;
            indices[o + 0] = b + 0; indices[o + 1] = b + 1; indices[o + 2] = b + 2;
            indices[o + 3] = b + 2; indices[o + 4] = b + 3; indices[o + 5] = b + 0;
        }

        return new MeshData { Vertices = verts, Indices = indices };
    }

    public static MeshData CreatePlane(float width = 1f, float depth = 1f, Vector4? color = null, Vector2 uvTiling = default)
    {
        Vector4 tint = color ?? Vector4.One;
        Vector2 tile = uvTiling == default ? Vector2.One : uvTiling;

        float hw = width * 0.5f;
        float hd = depth * 0.5f;

        var verts = new SimpleModelVertex[]
        {
            new() { Position = new(-hw, 0,-hd), Normal = Vector3.Up, Color = tint, TexCoord = new(0, 0) },
            new() { Position = new(-hw, 0, hd), Normal = Vector3.Up, Color = tint, TexCoord = new(0, tile.Y) },
            new() { Position = new( hw, 0, hd), Normal = Vector3.Up, Color = tint, TexCoord = new(tile.X, tile.Y) },
            new() { Position = new( hw, 0,-hd), Normal = Vector3.Up, Color = tint, TexCoord = new(tile.X, 0) },
        };

        var indices = new uint[] { 0, 1, 2, 2, 3, 0 };

        return new MeshData { Vertices = verts, Indices = indices };
    }

    public static MeshData CreateSphere(float radius = 0.5f, int slices = 24, int stacks = 16, Vector4? color = null)
    {
        Vector4 tint = color ?? Vector4.One;
        var verts = new List<SimpleModelVertex>((slices + 1) * (stacks + 1));

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

                verts.Add(new SimpleModelVertex
                {
                    Position = n * radius,
                    Normal = n,
                    Color = tint,
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

        return new MeshData { Vertices = verts.ToArray(), Indices = indices.ToArray() };
    }
}

public class MeshBuffers : IDisposable
{
    public VertexBuffer<SimpleModelVertex> Vertices { get; }
    public IndexBuffer Indices { get; }
    public int IndexCount => Indices.Count;

    public MeshBuffers(IGraphicsDevice device, MeshData data)
    {
        Vertices = new VertexBuffer<SimpleModelVertex>(device, data.Vertices.Length);
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