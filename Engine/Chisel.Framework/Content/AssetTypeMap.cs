using System;
using System.Collections.Generic;

namespace Chisel.Framework;

public static class AssetTypeMap
{
    private static readonly Dictionary<string, AssetType> _extMap = new Dictionary<string, AssetType>() {
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
        if (_extMap.TryGetValue(extension, out var type)) return type;
        return AssetType.None;
    }
}
