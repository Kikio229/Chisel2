using System;

namespace Chisel.Framework;

public struct MaterialTableDescription
{
    public IImage[] Textures;

    public MaterialTableDescription()
    {
        Textures = Array.Empty<IImage>();
    }
}
