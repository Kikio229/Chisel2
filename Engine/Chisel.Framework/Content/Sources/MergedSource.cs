using System;
using System.IO;

namespace Chisel.Framework;

public class MergedSource : Disposable, ISource
{
    private ISource _primary;
    private ISource _fallback;

    public MergedSource(ISource primary, ISource fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public bool Exists(string relativePath)
    {
        return _primary.Exists(relativePath) || _fallback.Exists(relativePath);
    }

    public Stream Open(string relativePath)
    {
        if (_primary.Exists(relativePath))
        {
            return _primary.Open(relativePath);
        }

        return _fallback.Open(relativePath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_primary is PackedSource psrc)
            { 
                psrc.Dispose();
            }

            if (_fallback is PackedSource fsrc)
            {
                fsrc.Dispose();
            }
        }
    }
}
