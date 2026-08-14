using Chisel.Framework;
using Chisel.Resource;
using Microsoft.Xna.Framework;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct SpriteVertex
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector2 UV;
    [Vertex(2)] public Vector4 Color;
}

public class SpriteBatch : IDisposable
{
    struct QueuedSprite
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector4 Color;
        public Vector2 UVMin;
        public Vector2 UVMax;
        public float Rotation;
        public Vector2 Origin;
    }

    IGraphicsDevice device;
    ShaderEffect shader;
    ShaderEffectParameter viewProjectionParam;
    ShaderEffectParameter textureParam;
    ShaderEffectParameter samplerParam;
    ISampler sampler;
    IGraphicsState pipelineState;

    VertexBuffer<SpriteVertex> vertexBuffer;
    IndexBuffer indexBuffer;
    List<QueuedSprite> sprites = new List<QueuedSprite>();

    Texture2D currentTexture;
    Rectangle? currentClip;
    int capacity;
    bool disposedValue;

    public SpriteBatch(IGraphicsDevice device, ContentManager content, int initialCapacity = 256)
    {
        this.device = device;
        shader = content.Load<ShaderEffect>("Shaders/Sprite");
        capacity = initialCapacity;

        viewProjectionParam = shader.Parameters["ViewProjection"];
        textureParam = shader.Parameters["DiffuseTexture"];
        samplerParam = shader.Parameters["DiffuseSampler"];

        sampler = device.CreateSampler(new SamplerDescription
        {
            FilterMode = SamplerFilterMode.Nearest,
            WrapMode = SamplerWrapMode.Clamp,
        });

        pipelineState = device.CreateGraphicsState(new GraphicsStateDescription
        {
            VertexShader = shader.CurrentTechnique.GetStage(ShaderStage.Vertex),
            PixelShader = shader.CurrentTechnique.GetStage(ShaderStage.Pixel),
            Topology = GraphicsTopology.TriangleList,
            BlendMode = GraphicsBlendMode.Alpha,
            VertexLayout = VertexLayoutCache.Get<SpriteVertex>(),
        });

        vertexBuffer = new VertexBuffer<SpriteVertex>(device, capacity * 4);
        indexBuffer = new IndexBuffer(device, capacity * 6);
        BuildIndices(capacity);
    }

    void BuildIndices(int spriteCapacity)
    {
        uint[] indices = new uint[spriteCapacity * 6];

        for (int i = 0; i < spriteCapacity; i++)
        {
            uint offset = (uint)(i * 4);
            int b = i * 6;

            indices[b + 0] = offset + 0;
            indices[b + 1] = offset + 1;
            indices[b + 2] = offset + 2;
            indices[b + 3] = offset + 2;
            indices[b + 4] = offset + 3;
            indices[b + 5] = offset + 0;
        }

        indexBuffer.SetData(indices);
    }
    public void Begin(Matrix viewProjection)
    {
        sprites.Clear();
        currentTexture = null;
        currentClip = null;
        viewProjectionParam.SetValue(viewProjection);

        device.Clear(GraphicsClearFlags.Depth, Color.Black, 1.0f, 0);
    }
    public void Draw(Texture2D texture, Vector2 position, Vector2 size, Color color, Rectangle? sourceRectangle = null, float rotation = 0f, Vector2 origin = default)
    {
        Vector2 uvMin;
        Vector2 uvMax;

        if (sourceRectangle.HasValue)
        {
            Rectangle rect = sourceRectangle.Value;
            uvMin = new Vector2(rect.Left / (float)texture.Width, rect.Top / (float)texture.Height);
            uvMax = new Vector2(rect.Right / (float)texture.Width, rect.Bottom / (float)texture.Height);
        }
        else
        {
            uvMin = Vector2.Zero;
            uvMax = Vector2.One;
        }

        QueueSprite(texture, position, size, color, uvMin, uvMax, rotation, origin);
    }
    public void Draw(RenderTarget2D renderTarget, Vector2 position, Vector2 size, Color color, float rotation = 0f, Vector2 origin = default)
    {
        Vector2 uvMin = Vector2.Zero;
        Vector2 uvMax = Vector2.One;

        if (device.Backend == GraphicsBackend.OpenGL)
        {
            // GL rasterizes into an FBO with the opposite row order D3D does, relative to
            // clip space. Sampling it with the same UVs as a loaded texture comes out
            // vertically mirrored unless this is compensated for.
            uvMin = new Vector2(0, 1);
            uvMax = new Vector2(1, 0);
        }

        QueueSprite(renderTarget.ColorTexture, position, size, color, uvMin, uvMax, rotation, origin);
    }
    public void SetClip(Rectangle? clip)
    {
        if (clip == currentClip)
        {
            return;
        }

        Flush();
        currentClip = clip;
    }
    void QueueSprite(Texture2D texture, Vector2 position, Vector2 size, Color color, Vector2 uvMin, Vector2 uvMax, float rotation, Vector2 origin)
    {
        if (currentTexture != null && currentTexture != texture)
        {
            Flush();
        }

        currentTexture = texture;

        sprites.Add(new QueuedSprite
        {
            Position = position,
            Size = size,
            Color = color.ToVector4(),
            UVMin = uvMin,
            UVMax = uvMax,
            Rotation = rotation,
            Origin = origin,
        });
    }
    public void End()
    {
        Flush();
    }

    void Flush()
    {
        if (sprites.Count == 0 || currentTexture == null)
        {
            sprites.Clear();
            return;
        }

        if (sprites.Count > capacity)
        {
            Grow(sprites.Count);
        }

        SpriteVertex[] vertices = new SpriteVertex[sprites.Count * 4];

        for (int i = 0; i < sprites.Count; i++)
        {
            QueuedSprite sprite = sprites[i];
            int v = i * 4;

            Vector2 local0 = new Vector2(0, 0);
            Vector2 local1 = new Vector2(sprite.Size.X, 0);
            Vector2 local2 = new Vector2(sprite.Size.X, sprite.Size.Y);
            Vector2 local3 = new Vector2(0, sprite.Size.Y);

            Vector2 p0, p1, p2, p3;

            if (sprite.Rotation == 0f)
            {
                Vector2 offset = sprite.Position - sprite.Origin;
                p0 = local0 + offset;
                p1 = local1 + offset;
                p2 = local2 + offset;
                p3 = local3 + offset;
            }
            else
            {
                float sin = MathF.Sin(sprite.Rotation);
                float cos = MathF.Cos(sprite.Rotation);

                p0 = RotateAndPlace(local0, sprite.Origin, sprite.Position, sin, cos);
                p1 = RotateAndPlace(local1, sprite.Origin, sprite.Position, sin, cos);
                p2 = RotateAndPlace(local2, sprite.Origin, sprite.Position, sin, cos);
                p3 = RotateAndPlace(local3, sprite.Origin, sprite.Position, sin, cos);
            }

            vertices[v + 0] = new SpriteVertex { Position = new Vector3(p0.X, p0.Y, 0), UV = new Vector2(sprite.UVMin.X, sprite.UVMin.Y), Color = sprite.Color };
            vertices[v + 1] = new SpriteVertex { Position = new Vector3(p1.X, p1.Y, 0), UV = new Vector2(sprite.UVMax.X, sprite.UVMin.Y), Color = sprite.Color };
            vertices[v + 2] = new SpriteVertex { Position = new Vector3(p2.X, p2.Y, 0), UV = new Vector2(sprite.UVMax.X, sprite.UVMax.Y), Color = sprite.Color };
            vertices[v + 3] = new SpriteVertex { Position = new Vector3(p3.X, p3.Y, 0), UV = new Vector2(sprite.UVMin.X, sprite.UVMax.Y), Color = sprite.Color };
        }

        vertexBuffer.SetData(vertices);

        device.BindGraphicsState(pipelineState);

        textureParam.SetValue(currentTexture.Image);
        samplerParam.SetValue(sampler);
        shader.CurrentTechnique.Apply();

        vertexBuffer.Bind(0);
        device.SetVertexLayout(vertexBuffer.Layout, 0);
        indexBuffer.Bind();

        if (currentClip.HasValue)
        {
            Rectangle clip = currentClip.Value;
            device.SetScissor(new Vector2(clip.X, clip.Y), new Vector2(clip.Width, clip.Height));
            device.SetScissorEnabled(true);
        }
        else
        {
            device.SetScissorEnabled(false);
        }

        device.DrawIndexed((uint)(sprites.Count * 6));

        sprites.Clear();
    }

    // Rotates a local corner around `origin` by the given sin/cos, then places it in world
    // space such that `origin` itself lands on `position`. With origin = Vector2.Zero this
    // reduces to `local + position`, matching the pre-rotation behavior exactly.
    static Vector2 RotateAndPlace(Vector2 local, Vector2 origin, Vector2 position, float sin, float cos)
    {
        Vector2 fromOrigin = local - origin;
        float x = fromOrigin.X * cos - fromOrigin.Y * sin;
        float y = fromOrigin.X * sin + fromOrigin.Y * cos;
        return new Vector2(x, y) + position;
    }

    void Grow(int minimumCapacity)
    {
        while (capacity < minimumCapacity)
        {
            capacity *= 2;
        }

        vertexBuffer.Dispose();
        indexBuffer.Dispose();

        vertexBuffer = new VertexBuffer<SpriteVertex>(device, capacity * 4);
        indexBuffer = new IndexBuffer(device, capacity * 6);
        BuildIndices(capacity);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                vertexBuffer.Dispose();
                indexBuffer.Dispose();

                if (sampler is IDisposable disposableSampler)
                {
                    disposableSampler.Dispose();
                }

                if (pipelineState is IDisposable disposablePipeline)
                {
                    disposablePipeline.Dispose();
                }
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}