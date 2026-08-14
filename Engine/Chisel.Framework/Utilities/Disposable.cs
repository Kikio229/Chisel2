using System;

namespace Chisel.Framework;

public abstract class Disposable : IDisposable
{
    public bool IsDisposed { get; private set; }
    string creationStackTrace = Environment.StackTrace;

    public Disposable()
    {
        IsDisposed = false;
    }

    ~Disposable()
    {
        Dispose(false);
    }

    protected abstract void Dispose(bool disposing);

    public void Dispose()
    {
        if (IsDisposed)
        {
            Logger.AppendWarn(GetType().Name + " was already disposed.");
            return;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
        IsDisposed = true;
    }
}
