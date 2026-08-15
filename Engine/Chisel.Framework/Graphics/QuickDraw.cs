using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

/// <summary>
/// Static abstraction for the GraphicsDevice that allows a more OpenGL style interface.
/// Not quite as fast as using the standard GraphicsDevice on it's own, but much simpler.
/// </summary>
public static class QuickDraw
{
    // Obviously we need to store the device. This will be set when the window inits.
    // This means it's forever bound to the single Game instance.
    static IGraphicsDevice device;

    internal static void Init(IGraphicsDevice _device)
    {
        device = _device;
    }

    // State
    static GraphicsStateDescription requestedState = new GraphicsStateDescription();
    static ShaderPass currentShader;

    // State cache, using a custom comparer to not mess with the barebones struct
    static Dictionary<GraphicsStateDescription, IGraphicsState> stateCache =
       new Dictionary<GraphicsStateDescription, IGraphicsState>(new GraphicsStateDescriptionComparer());

    static class UserVertexStorage<T> where T : unmanaged
    {
        public static VertexBuffer<T> Buffer;
        public static int Capacity;
    }

    static IndexBuffer userIndexBuffer;
    static int userIndexCapacity;

    public static void SetShader(ShaderPass shader)
    {
        currentShader = shader;
        requestedState.VertexShader = shader?.GetStage(ShaderStage.Vertex);
        requestedState.PixelShader = shader?.GetStage(ShaderStage.Pixel);
    }

    // Stateful functions

    public static void SetBlendMode(GraphicsBlendMode mode) => requestedState.BlendMode = mode;
    public static void SetCullMode(GraphicsCullMode mode) => requestedState.CullMode = mode;
    public static void SetDepthMode(GraphicsDepthMode mode) => requestedState.DepthMode = mode;
    public static void SetFillMode(GraphicsFillMode mode) => requestedState.FillMode = mode;
    public static void SetTopology(GraphicsTopology topology) => requestedState.Topology = topology;
    public static void SetDepthWrite(bool allow) => requestedState.AllowDepthWrite = allow;

    // VB stuff
    public static void BindVertexBuffer<T>(VertexBuffer<T> vertexBuffer, uint slot = 0) where T : unmanaged
    {
        vertexBuffer.Bind(slot);
        device.SetVertexLayout(vertexBuffer.Layout, slot);
        requestedState.VertexLayout = vertexBuffer.Layout;
    }

    public static void BindIndexBuffer(IndexBuffer indexBuffer)
    {
        indexBuffer.Bind();
    }


    // Draw functions

    public static void Draw(uint vtxCount)
    {
        EnsureStateBound();
        device.Draw(vtxCount);
    }
    public static void DrawIndexed(uint idxCount)
    {
        EnsureStateBound();
        device.DrawIndexed(idxCount);
    }
    public static void DrawInstanced(uint vtxCount, uint instCount)
    {
        EnsureStateBound();
        device.DrawInstanced(vtxCount, instCount);
    }
    public static void DrawIndexedInstanced(uint idxCount, uint instCount)
    {
        EnsureStateBound();
        device.DrawIndexedInstanced(idxCount, instCount);
    }

    public static void DrawUserPrimitives<T>(ReadOnlySpan<T> vertices, uint slot = 0) where T : unmanaged
    {
        EnsureUserVertexCapacity<T>(vertices.Length);
        UserVertexStorage<T>.Buffer.SetData(vertices);
        BindVertexBuffer(UserVertexStorage<T>.Buffer, slot);
        Draw((uint)vertices.Length);
    }

    public static void DrawUserIndexedPrimitives<T>(ReadOnlySpan<T> vertices, ReadOnlySpan<uint> indices, uint slot = 0) where T : unmanaged
    {
        EnsureUserVertexCapacity<T>(vertices.Length);
        EnsureUserIndexCapacity(indices.Length);

        UserVertexStorage<T>.Buffer.SetData(vertices);
        userIndexBuffer.SetData(indices);

        BindVertexBuffer(UserVertexStorage<T>.Buffer, slot);
        BindIndexBuffer(userIndexBuffer);

        DrawIndexed((uint)indices.Length);
    }
    static void EnsureUserVertexCapacity<T>(int count) where T : unmanaged
    {
        if (UserVertexStorage<T>.Buffer == null || UserVertexStorage<T>.Capacity < count)
        {
            UserVertexStorage<T>.Buffer?.Dispose();

            int newCapacity = UserVertexStorage<T>.Capacity == 0 ? 64 : UserVertexStorage<T>.Capacity;
            while (newCapacity < count) newCapacity *= 2;

            UserVertexStorage<T>.Buffer = new VertexBuffer<T>(device, newCapacity);
            UserVertexStorage<T>.Capacity = newCapacity;
        }
    }

    static void EnsureUserIndexCapacity(int count)
    {
        if (userIndexBuffer == null || userIndexCapacity < count)
        {
            userIndexBuffer?.Dispose();

            int newCapacity = userIndexCapacity == 0 ? 64 : userIndexCapacity;
            while (newCapacity < count) newCapacity *= 2;

            userIndexBuffer = new IndexBuffer(device, newCapacity);
            userIndexCapacity = newCapacity;
        }
    }

    static void EnsureStateBound()
    {
        requestedState.ColorFormats = device.ColorFormats;
        requestedState.DepthStencilFormat = device.DepthStencilFormat;
        requestedState.SampleCount = device.SampleCount;

        if (!stateCache.TryGetValue(requestedState, out IGraphicsState state))
        {
            state = device.CreateGraphicsState(requestedState);
            stateCache.Add(requestedState, state);
        }

        device.BindGraphicsState(state);
        currentShader?.Apply();
    }

    // The actual comparer for states. This avoids us doing unecessary state changes.
    sealed class GraphicsStateDescriptionComparer : IEqualityComparer<GraphicsStateDescription>
    {
        public bool Equals(GraphicsStateDescription a, GraphicsStateDescription b)
        {
            return ReferenceEquals(a.VertexShader, b.VertexShader)
                && ReferenceEquals(a.PixelShader, b.PixelShader)
                && a.Topology == b.Topology
                && a.DepthMode == b.DepthMode
                && a.BlendMode == b.BlendMode
                && a.CullMode == b.CullMode
                && a.FillMode == b.FillMode
                && a.AllowDepthWrite == b.AllowDepthWrite
                && a.DepthStencilFormat == b.DepthStencilFormat
                && a.SampleCount == b.SampleCount
                && FormatsEqual(a.ColorFormats, b.ColorFormats)
                && LayoutsEqual(a.VertexLayout, b.VertexLayout);
        }

        public int GetHashCode(GraphicsStateDescription d)
        {
            HashCode hash = new HashCode();
            hash.Add(d.VertexShader);
            hash.Add(d.PixelShader);
            hash.Add(d.Topology);
            hash.Add(d.DepthMode);
            hash.Add(d.BlendMode);
            hash.Add(d.CullMode);
            hash.Add(d.FillMode);
            hash.Add(d.AllowDepthWrite);
            hash.Add(d.DepthStencilFormat);
            hash.Add(d.SampleCount);

            if (d.ColorFormats != null)
            {
                foreach (ImageFormat format in d.ColorFormats)
                    hash.Add(format);
            }

            hash.Add(d.VertexLayout.Stride);
            if (d.VertexLayout.Attributes != null)
            {
                foreach (VertexAttributeDescription attr in d.VertexLayout.Attributes)
                {
                    hash.Add(attr.Location);
                    hash.Add(attr.Format);
                    hash.Add(attr.Offset);
                }
            }

            return hash.ToHashCode();
        }

        static bool FormatsEqual(ImageFormat[] a, ImageFormat[] b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            return a.AsSpan().SequenceEqual(b);
        }

        static bool LayoutsEqual(VertexLayoutDescription a, VertexLayoutDescription b)
        {
            if (a.Stride != b.Stride) return false;

            // VertexLayoutCache.Get<T>() caches one Attributes array per Type and never
            // re-reflects, so two VertexBuffer<T>s of the same T always share the exact same
            // array instance.
            if (a.Attributes == b.Attributes) return true;

            if (a.Attributes == null || b.Attributes == null) return false;
            if (a.Attributes.Length != b.Attributes.Length) return false;

            for (int i = 0; i < a.Attributes.Length; i++)
            {
                if (a.Attributes[i].Location != b.Attributes[i].Location
                    || a.Attributes[i].Format != b.Attributes[i].Format
                    || a.Attributes[i].Offset != b.Attributes[i].Offset)
                {
                    return false;
                }
            }

            return true;
        }
    }
}