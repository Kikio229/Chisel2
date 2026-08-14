using System;

namespace Chisel.Framework;

public abstract class Disposable : IDisposable
{
    public bool IsDisposed { get; private set; }
#if DEBUG
    readonly string creationStackTrace = Environment.StackTrace;
#endif

    public Disposable()
    {
        IsDisposed = false;
    }

    ~Disposable()
    {
        if (!IsDisposed)
        {
#if DEBUG
            Logger.AppendWarn($"{GetType().Name} was garbage collected without Dispose() being called, native resource leaked. Created at:\n{creationStackTrace}");
#else
            Logger.AppendWarn($"{GetType().Name} was garbage collected without Dispose() being called, native resource leaked. Run in Debug for a creation stack trace.");
#endif
        }
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
