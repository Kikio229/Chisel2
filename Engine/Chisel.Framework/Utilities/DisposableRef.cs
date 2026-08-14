using System;

namespace Chisel.Framework;

public abstract class DisposableRef : IDisposable
{
    public int RefCount { get; private set; }
    public bool IsDisposed { get; private set; }

    public DisposableRef()
    {
        RefCount = 1;
        IsDisposed = false;
    }

    ~DisposableRef()
    {
        Dispose();
    }

    public void AddRef()
    {
        RefCount++;
    }

    protected abstract void Dispose(bool disposing);

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        RefCount--;

        if (RefCount <= 0)
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }
    }
}
