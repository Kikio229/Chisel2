using System.Collections.Generic;

namespace Chisel.Resource.Builder;
static class AssetHandlerRegistry
{
    static readonly Dictionary<AssetType, IAssetHandler> handlers = new Dictionary<AssetType, IAssetHandler>
    {
        [AssetType.Copy] = new RawHandler(),
        [AssetType.Shader] = new ShaderAssetHandler(new D3DShaderCompiler(), new GLShaderCompiler()),
    };

    public static IAssetHandler Resolve(AssetType type)
    {
        if (handlers.TryGetValue(type, out IAssetHandler handler))
        {
            return handler;
        }

        return handlers[AssetType.Copy];
    }
}