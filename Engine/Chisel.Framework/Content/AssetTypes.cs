using System;
using System.Collections.Generic;
using System.Text;

namespace Chisel.Resource;

public enum AssetType
{
    Texture,
    Shader,
    Model,
    Sound,
    Copy,
    None,
}

public static class AssetTypeMap
{
    static readonly Dictionary<string, AssetType> extensionsMap = new Dictionary<string, AssetType>()
    {
        [".png"] = AssetType.Texture,
        [".jpg"] = AssetType.Texture,
        [".dds"] = AssetType.Texture,
        [".hdr"] = AssetType.Copy,
        [".wav"] = AssetType.Sound,
        [".ogg"] = AssetType.Sound,
        [".csl"] = AssetType.Shader,
        [".hlsl"] = AssetType.Shader,
        [".ccmdl"] = AssetType.Model,
        [".script"] = AssetType.Copy,
        [".txt"] = AssetType.Copy,
        [".cmt"] = AssetType.Copy,
        [".ctt"] = AssetType.Copy,
        [".ttf"] = AssetType.Copy,
        [".choreo"] = AssetType.Copy,
        [".morph"] = AssetType.Copy,
        [".scene"] = AssetType.Copy,
        [".cmap"] = AssetType.Copy,
        [".clm"] = AssetType.Copy,
    };

    public static AssetType Resolve(string extension)
    {
        if (extensionsMap.TryGetValue(extension, out var type)) return type;
        return AssetType.None;
    }
}