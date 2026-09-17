using System;

namespace Chisel.Framework;

internal class GLMaterialTable : Disposable, IMaterialTable
{
    public IImage[] Textures => (IImage[])TexturesInternal;

    internal GLImage[] TexturesInternal { get; set; }

    public GLMaterialTable(GLImage[] textures)
    {
        TexturesInternal = textures;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (GLImage t in TexturesInternal)
            {
                t.Dispose();
            }
        }
    }
}