using System;

namespace Chisel.Framework;

internal class GLMaterialTable : Disposable, IMaterialTable
{
    public IImage[] Textures { get; }

    public GLMaterialTable(IImage[] textures)
    {
        Textures = textures;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (GLImage t in Textures)
            {
                t.Dispose();
            }
        }
    }
}