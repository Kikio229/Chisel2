using Chisel.Framework;
using Chisel.Framework.UI;
using Chisel.Resource;
using FontStashSharp;
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
    struct QueuedQuad
    {
        public SpriteVertex V0, V1, V2, V3;
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
    List<QueuedQuad> quads = new List<QueuedQuad>();

    FFRenderer2 fontRenderer;
    FontSystem fontSystem;

    Texture2D currentTexture;
    Rectangle? currentClip;
    int capacity;
    int vertexCapacity;
    int frameVertexOffset;
    List<VertexBuffer<SpriteVertex>> pendingVertexBufferDisposal = new List<VertexBuffer<SpriteVertex>>();
    bool disposedValue;

    public SpriteBatch(IGraphicsDevice device, ContentManager content, int initialCapacity = 256)
    {
        this.device = device;
        shader = content.Load<ShaderEffect>("Shaders/Sprite");
        capacity = initialCapacity;
        vertexCapacity = capacity * 4;

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

        fontRenderer = new FFRenderer2(this);
        fontSystem = new FontSystem();
        fontSystem.AddFont(content.LoadBytes("Fonts/default.ttf"));

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
    public void Begin(Matrix4 viewProjection)
    {
        foreach (VertexBuffer<SpriteVertex> old in pendingVertexBufferDisposal)
        {
            old.Dispose();
        }
        pendingVertexBufferDisposal.Clear();

        frameVertexOffset = 0;
        quads.Clear();
        currentTexture = null;
        currentClip = null;
        viewProjectionParam.SetValue(viewProjection);

        device.Clear(Color.Black, 1.0f, 0, GraphicsClearFlags.Depth);
    }
    public void DrawString(string text, int fontSize, Vector2 position, Color color)
    {
        var fnt = fontSystem.GetFont(fontSize);

        fnt.DrawText(fontRenderer, text, position.ToNumerics(), new FSColor(color.R, color.G, color.B, color.A));
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

        if (device.Backend == GraphicsBackend.OpenGL46)
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
        Vector4 col = color.ToVector4Normalized();

        Vector2 local0 = Vector2.Zero;
        Vector2 local1 = new Vector2(size.X, 0);
        Vector2 local2 = new Vector2(size.X, size.Y);
        Vector2 local3 = new Vector2(0, size.Y);

        Vector2 p0, p1, p2, p3;

        if (rotation == 0f)
        {
            Vector2 offset = position - origin;
            p0 = local0 + offset;
            p1 = local1 + offset;
            p2 = local2 + offset;
            p3 = local3 + offset;
        }
        else
        {
            float sin = MathF.Sin(rotation);
            float cos = MathF.Cos(rotation);

            p0 = RotateAndPlace(local0, origin, position, sin, cos);
            p1 = RotateAndPlace(local1, origin, position, sin, cos);
            p2 = RotateAndPlace(local2, origin, position, sin, cos);
            p3 = RotateAndPlace(local3, origin, position, sin, cos);
        }

        AddQuad(texture,
            new SpriteVertex { Position = new Vector3(p0.X, p0.Y, 0), UV = new Vector2(uvMin.X, uvMin.Y), Color = col },
            new SpriteVertex { Position = new Vector3(p1.X, p1.Y, 0), UV = new Vector2(uvMax.X, uvMin.Y), Color = col },
            new SpriteVertex { Position = new Vector3(p2.X, p2.Y, 0), UV = new Vector2(uvMax.X, uvMax.Y), Color = col },
            new SpriteVertex { Position = new Vector3(p3.X, p3.Y, 0), UV = new Vector2(uvMin.X, uvMax.Y), Color = col });
    }

    public void DrawQuad(Texture2D texture, SpriteVertex v0, SpriteVertex v1, SpriteVertex v2, SpriteVertex v3)
    {
        AddQuad(texture, v0, v1, v2, v3);
    }

    void AddQuad(Texture2D texture, SpriteVertex v0, SpriteVertex v1, SpriteVertex v2, SpriteVertex v3)
    {
        if (currentTexture != null && currentTexture != texture)
        {
            Flush();
        }

        currentTexture = texture;
        quads.Add(new QueuedQuad { V0 = v0, V1 = v1, V2 = v2, V3 = v3 });
    }
    public void End()
    {
        Flush();
    }

    void Flush()
    {
        if (quads.Count == 0 || currentTexture == null)
        {
            quads.Clear();
            return;
        }

        if (quads.Count > capacity)
        {
            GrowIndices(quads.Count);
        }

        int neededVertices = frameVertexOffset + quads.Count * 4;

        if (neededVertices > vertexCapacity)
        {
            GrowVertexBuffer(neededVertices);
        }

        SpriteVertex[] vertices = new SpriteVertex[quads.Count * 4];

        for (int i = 0; i < quads.Count; i++)
        {
            QueuedQuad q = quads[i];
            int v = i * 4;
            vertices[v + 0] = q.V0;
            vertices[v + 1] = q.V1;
            vertices[v + 2] = q.V2;
            vertices[v + 3] = q.V3;
        }

        vertexBuffer.SetData(vertices, frameVertexOffset);

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

        device.DrawIndexed((uint)(quads.Count * 6), 0, frameVertexOffset);

        frameVertexOffset += quads.Count * 4;
        quads.Clear();
    }

    static Vector2 RotateAndPlace(Vector2 local, Vector2 origin, Vector2 position, float sin, float cos)
    {
        Vector2 fromOrigin = local - origin;
        float x = fromOrigin.X * cos - fromOrigin.Y * sin;
        float y = fromOrigin.X * sin + fromOrigin.Y * cos;
        return new Vector2(x, y) + position;
    }

    void GrowIndices(int minimumQuadCapacity)
    {
        while (capacity < minimumQuadCapacity)
        {
            capacity *= 2;
        }

        indexBuffer.Dispose();
        indexBuffer = new IndexBuffer(device, capacity * 6);
        BuildIndices(capacity);
    }

    void GrowVertexBuffer(int minimumVertexCapacity)
    {
        int newCapacity = vertexCapacity;

        while (newCapacity < minimumVertexCapacity)
        {
            newCapacity *= 2;
        }

        pendingVertexBufferDisposal.Add(vertexBuffer);
        vertexBuffer = new VertexBuffer<SpriteVertex>(device, newCapacity);
        vertexCapacity = newCapacity;
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