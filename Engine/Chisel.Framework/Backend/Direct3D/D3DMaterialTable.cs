using System;
using System.Runtime.CompilerServices;
using Vortice.Win32.Graphics.Direct3D12;

namespace Chisel.Framework;
internal class D3DMaterialTable : IMaterialTable
{
    public GpuDescriptorHandle SrvTable { get; internal set; }
    internal uint RelativeSlot;
    public IImage[] Textures { get; }
    internal D3DImage[] TextureImages;
}
internal readonly struct MaterialCacheKey : IEquatable<MaterialCacheKey>
{
    readonly IImage[] textures;

    public MaterialCacheKey(IImage[] textures)
    {
        this.textures = (IImage[])textures.Clone();
    }

    public bool Equals(MaterialCacheKey other)
    {
        if (textures.Length != other.textures.Length)
        {
            return false;
        }

        for (int i = 0; i < textures.Length; i++)
        {
            if (!ReferenceEquals(textures[i], other.textures[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        return obj is MaterialCacheKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        foreach (IImage texture in textures)
        {
            hash.Add(RuntimeHelpers.GetHashCode(texture));
        }

        return hash.ToHashCode();
    }
}