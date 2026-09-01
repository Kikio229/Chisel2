using System;
using System.IO;
using System.Threading.Tasks;

namespace Chisel.Framework;

public class Game
{
    public Window Window { get; private set; }
    public static Game? Instance { get; private set; }
    public IGraphicsDevice GraphicsDevice { get; private set; }
    public ContentManager Content { get; private set; }

    private bool _isDebug;

    public Game(GraphicsBackend backend, bool debug)
    {
        AppDomain.CurrentDomain.UnhandledException += DomainUnhandled;
        TaskScheduler.UnobservedTaskException += SchedulerUnhandled;

        Window = new Window(backend, debug);

        Window.Startup += OnStartup;
        Window.TickUpdate += OnTickUpdate;
        Window.FrameUpdate += (d)=>
        {
            OnFrameUpdate(d);

            GraphicsDevice?.BeginFrame();
            OnDrawFrame(d);
            GraphicsDevice?.EndFrame();
        };
        Window.Shutdown += OnShutdown;

        Window.Resize += (int w, int h) =>
        {
            OnWindowResize(w, h);
            GraphicsDevice?.Resize(w, h);
        };
        Window.Reposition += OnWindowReposition;
        Window.FocusGain += OnWindowFocusGain;
        Window.FocusLost += OnWindowFocusLost;

        _isDebug = debug;

        if (Instance != null)
        {
            throw new Exception("Only one game instance can exist at a time!");
        }

        Instance = this;
    }

    public void Run(string[] args)
    {
        Window?.InitAndRun();
        Window?.Dispose();
    }

    protected virtual void OnStartup()
    {

        switch (Window.Backend)
        {
            case GraphicsBackend.Vulkan:
                GraphicsDevice = new VKGraphicsDevice(_isDebug);
                break;

            case GraphicsBackend.OpenGL:
                GraphicsDevice = new GLGraphicsDevice(Window.GLContext, _isDebug);
                break;
        }

        // Detect if we're using .cpk or not
        bool isPacked = File.Exists(Path.Combine(AppContext.BaseDirectory, "Content", "assets.cpk"));
        ISource mainSrc = isPacked ?
            new PackedSource(Path.Combine(AppContext.BaseDirectory, "Content", "assets.cpk")) :
            new LooseSource(Path.Combine(AppContext.BaseDirectory, "Content"));

        Content = new ContentManager(
            new MergedSource(new LooseSource(Path.Combine(AppContext.BaseDirectory, "Mods")), mainSrc));

        Content.RegisterLoader(new ShaderPassLoader(GraphicsDevice));
        Content.RegisterLoader(new ShaderEffectLoader(GraphicsDevice));
        Content.RegisterLoader(new TextureLoader(GraphicsDevice));

        // Init QuickDraw
        QuickDraw.Init(GraphicsDevice);
    }

    protected virtual void OnTickUpdate(double delta)
    {

    }

    protected virtual void OnFrameUpdate(double delta)
    {

    }

    protected virtual void OnDrawFrame(double delta)
    {

    }

    protected virtual void OnShutdown()
    {

    }

    protected virtual void OnWindowResize(int width, int height)
    {

    }

    protected virtual void OnWindowReposition(int offsetX, int offsetY)
    {

    }

    protected virtual void OnWindowFocusGain()
    {

    }

    protected virtual void OnWindowFocusLost()
    {

    }

    private void NotifyOfUnhandled(Exception ex)
    {
#if !DEBUG
        Window?.ThrowMessage($"Engine Error", $"The program has encountered a fatal error and cannot continue.\n{ex.ToString()}");
#endif
    }

    private void DomainUnhandled(object? sender, UnhandledExceptionEventArgs ex)
    {
        NotifyOfUnhandled((Exception)ex.ExceptionObject);
    }

    private void SchedulerUnhandled(object? sender, UnobservedTaskExceptionEventArgs ex)
    {
        NotifyOfUnhandled(ex.Exception);
        ex.SetObserved();
    }
}