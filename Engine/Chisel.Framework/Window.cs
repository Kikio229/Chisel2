using Chisel.Framework.Utilities;
using Chisel.Resource;
using Hexa.NET.SDL3;
using System;
using System.Runtime.InteropServices;

namespace Chisel.Framework;

[Flags]
public enum CursorMode
{
    Normal = 0,
    Confined = 1 << 0,
    Locked = 1 << 1,
    Hidden = 1 << 2,
}

public class Window : Disposable
{
    public string Title { get; private set; }
    public uint TickRate { get; private set; }
    public uint FrameRate { get; private set; }
    public (int X, int Y) Position { get; private set; }
    public (int W, int H) Resolution { get; private set; }
    public float Opacity { get; private set; }
    public int Display { get; private set; }
    public bool IsTickOn { get; private set; }
    public bool IsVsyncOn { get; private set; }
    public bool IsFullscreen { get; private set; }
    public bool IsMinimized { get; private set; }
    public bool IsMaximized { get; private set; }
    public CursorMode CursorMode { get; private set; }
    public bool IsFocused { get; private set; } = true;

    public GraphicsBackend Backend { get; set; } = GraphicsBackend.Direct3D12;

    // GL is weird and has to know it's allowed here before anything
    public unsafe SDLGLContext GLContext { get; private set; }
    public bool IsDebug { get; set; }

    internal unsafe SDLWindow* Handle { get; private set; }
    internal event Action? Startup, Shutdown, FocusGain, FocusLost;
    internal event Action<int, int>? Resize, Reposition;
    internal event Action<double>? TickUpdate, FrameUpdate;
    public event Action<string>? TextInput;

    internal event Action<SDLEvent> OnSDLEvent;

    // Max frame or tick rate. Mostly for framerate becuase very high framerates seems
    // to cause a bunch of inaccuracy with the timings
    private const uint _updateMax = 1024;
    private bool _isActive;

    // For debug mode
    private readonly FpsCounter fpsCounter = new();
    private double titleUpdateTimer;
    private readonly string baseTitle;

    public Window(GraphicsBackend backend = GraphicsBackend.Auto, bool debug = false)
    {
        Title = "Default Window";
        baseTitle = Title;
        TickRate = 60;
        FrameRate = 60;
        Position = new((int)SDL.SDL_WINDOWPOS_CENTERED_MASK, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK);
        Resolution = new(1280, 720);
        Display = 0;
        Opacity = 1.0f;

        FocusGain += () => IsFocused = true;
        FocusLost += () => IsFocused = false;

        this.IsDebug = debug;

        Backend = backend == GraphicsBackend.Auto ? ResolveAutoBackend() : backend;
    }

    // TODO: search GPUs and find the best that way
    static GraphicsBackend ResolveAutoBackend()
    {
#if WINDOWS
        return GraphicsBackend.Direct3D12;
#else
        return GraphicsBackend.OpenGL;
#endif
    }
    public unsafe void InitAndRun()
    {
        SDLWindowFlags flags = 0;

        // Chcking for high DPI support
        if (SDL.GetWindowDisplayScale(Handle) > 1.0f &&
            SDL.GetWindowPixelDensity(Handle) > 1.0f)
        {
            flags |= SDLWindowFlags.HighPixelDensity;
        }

        // GL has to init BEFORE the window
        if (Backend == GraphicsBackend.OpenGL)
        {
            SDL.GLSetAttribute(SDLGLAttr.ContextProfileMask, 0x0001); // Core
            SDL.GLSetAttribute(SDLGLAttr.ContextMajorVersion, 3);
            SDL.GLSetAttribute(SDLGLAttr.ContextMinorVersion, 3);
            SDL.GLSetAttribute(SDLGLAttr.DepthSize, 24);
            SDL.GLSetAttribute(SDLGLAttr.StencilSize, 8);
            SDL.GLSetAttribute(SDLGLAttr.Doublebuffer, 1);
            flags |= SDLWindowFlags.Opengl;
            if (IsDebug)
            {
                SDL.GLSetAttribute(SDLGLAttr.ContextFlags, (int)0x0001);
            }
        }

        flags |= SDLWindowFlags.Resizable;

        Handle = SDL.CreateWindow(Title, Resolution.W, Resolution.H, (uint)flags);

        if (Backend == GraphicsBackend.OpenGL)
        {
            GLContext = SDL.GLCreateContext(Handle);
            SDL.GLMakeCurrent(Handle, GLContext);
        }

        SDL.InitSubSystem((uint)SDLInitFlags.Gamepad); // Also initalizes the event and joystick subsystems
        _isActive = true;

        ulong frequency = SDL.GetPerformanceFrequency();
        ulong current = 0;
        ulong previous = 0;

        double time = 0;
        double delta = 0;
        double tickDelta = 1.0 / TickRate; // Target tick delta
        double frameDelta = 1.0 / FrameRate; // Target frame delta

        double accumulator = 0;
        double nextFrame = 0;

        Startup?.Invoke();

        while (_isActive)
        {
            current = SDL.GetPerformanceCounter();
            time = (double)(current / frequency);
            delta = (double)(current - previous) / frequency;
            previous = current;

            if (nextFrame == 0)
            {
                nextFrame = time + frameDelta;
            }

            // Important we clear this before polling
            InputManager.Reset();

            SDLEvent ev;

            while (SDL.PollEvent(&ev))
            {
                switch ((SDLEventType)ev.Type)
                {
                    case SDLEventType.Quit:
                        _isActive = false;
                        break;

                    case SDLEventType.WindowResized:
                        RecalculateWindowDimensions();
                        break;

                    case SDLEventType.WindowMoved:
                        RecalculateWindowDimensions();
                        break;

                    case SDLEventType.WindowFocusGained:
                        FocusGain?.Invoke();
                        break;

                    case SDLEventType.WindowFocusLost:
                        FocusLost?.Invoke();
                        break;

                    case SDLEventType.TextInput:
                        TextInput?.Invoke(Marshal.PtrToStringUTF8((nint)ev.Text.Text) ?? string.Empty);
                        break;
                }
                OnSDLEvent?.Invoke(ev);
                InputManager.Process(ev);
            }

            accumulator += delta;

            if (IsTickOn)
            {
                // Very very important we don't miss any ticks
                while (accumulator >= tickDelta)
                {
                    TickUpdate?.Invoke(tickDelta);
                    accumulator -= tickDelta;
                }
            }

            // That being said, dropping frames is okay
            FrameUpdate?.Invoke(delta);

            // Show an approx FPS
            if(IsDebug)
            {
                fpsCounter.Update(delta);

                titleUpdateTimer += delta;
                if (titleUpdateTimer >= 0.25)
                {
                    titleUpdateTimer = 0.0;
                    Title = $"{baseTitle} - {fpsCounter.AverageFps:F0} FPS";
                    SDL.SetWindowTitle(Handle,Title);
                }
            }

            if (tickDelta != 1.0 / TickRate)
            {
                tickDelta = 1.0 / TickRate;
            }

            if (frameDelta != 1.0 / FrameRate)
            {
                frameDelta = 1.0 / FrameRate;
            }

            // Attempting to halt if were ahead of schedule
            // (not technically 100% accurate but it's close enough most of the time)
            if (IsVsyncOn)
            {
                nextFrame += frameDelta;

                while (true)
                {
                    ulong wait = SDL.GetPerformanceCounter();
                    double now = (double)wait / frequency;
                    double remain = nextFrame - now;

                    if (remain <= 0)
                    {
                        break;
                    }

                    if (remain > 0.001)
                    {
                        ulong ns = (ulong)((remain - 0.0005) * 1e+9);
                        SDL.DelayPrecise(ns);
                    }
                }
            }
            else
            {
                nextFrame = (double)SDL.GetPerformanceFrequency() / frequency;
            }
        }

        Shutdown?.Invoke();
    }

    public void Close()
    {
        _isActive = false;
    }

    public unsafe void ThrowMessage(string title, string message)
    {
        SDLMessageBoxButtonData* buttons = stackalloc SDLMessageBoxButtonData[2]
        {
            new SDLMessageBoxButtonData()
            {
                Flags = (uint)SDLMessageBoxButtonFlags.ReturnkeyDefault,
                ButtonID = 0,
                Text = (byte*)Marshal.StringToHGlobalAnsi("Copy")
            },
            new SDLMessageBoxButtonData()
            {
                Flags = (uint)SDLMessageBoxButtonFlags.EscapekeyDefault,
                ButtonID = 1,
                Text = (byte*)Marshal.StringToHGlobalAnsi("OK")
            }
        };

        SDLMessageBoxData box = new SDLMessageBoxData()
        {
            Flags = (uint)SDLMessageBoxFlags.Error,
            Title = (byte*)Marshal.StringToHGlobalAnsi(title),
            Message = (byte*)Marshal.StringToHGlobalAnsi(message),
            Window = Handle,
            Numbuttons = 2,
            Buttons = buttons,
            ColorScheme = null
        };

        int pressed;

        SDL.ShowMessageBox(&box, &pressed);

        if (pressed == 0)
        {
            SDL.SetClipboardText(message);
        }
    }

    public unsafe void SetTitle(string title)
    {
        if (!string.Equals(Title, title))
        {
            SDL.SetWindowTitle(Handle, title);
        }
    }

    public void SetTickRate(uint tps)
    {
        if (TickRate != tps)
        {
            if (tps > 0)
            {
                TickRate = tps;
            }
            else
            {
                TickRate = _updateMax;
            }
        }
    }

    public void SetFrameRate(uint fps)
    {
        if (FrameRate != fps)
        {
            if (fps > 0)
            {
                FrameRate = fps;
            }
            else
            {
                FrameRate = _updateMax;
            }
        }
    }

    public unsafe void SetPosition(int offsetX, int offsetY)
    {
        if (Position.X != offsetX || Position.Y != offsetY)
        {
            SDL.SetWindowPosition(Handle, offsetX, offsetY);
            Position = new(offsetX, offsetY);
        }
    }

    public unsafe void SetResolution(int width, int height)
    {
        if (Resolution.W != width || Resolution.H != height)
        {
            SDL.SetWindowSize(Handle, width, height);
            Center(); // ... It looks nicer
            Resolution = new(width, height);
        }
    }

    public unsafe void SetOpacity(float opacity)
    {
        if (Opacity != opacity)
        {
            SDL.SetWindowOpacity(Handle, opacity);
        }
    }

    public unsafe void SetDisplay(int display)
    {
        if (Display != display)
        {
            int count;
            uint* displays = SDL.GetDisplays(&count);
            int valid = count - 1; // Arrays start from 0, obviously
            bool maximized = IsMaximized;

            if (displays == null || valid < display)
            {
                Logger.AppendWarn($"Failed to set window to display [{display}], as the array of valid displays only goes up to {valid}");
                return;
            }

            uint target = displays[display];

            if (SDL.GetDisplayProperties(target) != 0)
            {
                SDLRect bounds;

                if (SDL.GetDisplayBounds(target, &bounds))
                {
                    if (Resolution.W >= bounds.W || Resolution.H >= bounds.H)
                    {
                        SetPosition(bounds.W, bounds.H);
                        Center();
                    }
                    else
                    {
                        int x = bounds.X + (bounds.W - Resolution.W) / 2;
                        int y = bounds.Y + (bounds.H - Resolution.H) / 2;
                        SetPosition(x, y);
                        Center();
                    }
                }
            }

            Display = display;
        }
    }

    public unsafe void SetFullscreen(bool enabled)
    {
        if (IsFullscreen != enabled)
        {
            uint current = SDL.GetDisplayForWindow(Handle);

            if (SDL.GetDisplayProperties(current) != 0)
            {
                SDL.SetWindowFullscreen(Handle, enabled);
                IsFullscreen = enabled;
            }

            RecalculateWindowDimensions();
        }
    }

    public unsafe void SetMinimized(bool enabled)
    {
        if (!IsMinimized)
        {
            SDL.MinimizeWindow(Handle);
            IsMinimized = true;
        }
        else
        {
            Restore();
        }

        RecalculateWindowDimensions();
    }

    public unsafe void SetMaximized(bool enabled)
    {
        if (!IsMaximized)
        {
            SDL.MaximizeWindow(Handle);
            IsMaximized = true;
        }
        else
        {
            Restore();
        }

        RecalculateWindowDimensions();
    }

    unsafe void RecalculateWindowDimensions()
    {
        int w, h;
        int x, y;
        SDL.GetWindowSizeInPixels(Handle,&w,&h);
        SDL.GetWindowPosition(Handle,&x,&y);

        Position = (x,y);
        Resolution = (w,h);
        Resize(w,h);
        Reposition(x,y);
    }
    public unsafe void StartTextInput() => SDL.StartTextInput(Handle);
    public unsafe void StopTextInput() => SDL.StopTextInput(Handle);
    public void SetTickMode(bool enabled)
    {
        IsTickOn = enabled;
    }

    public unsafe void SetCursorMode(CursorMode mode)
    {
        if (CursorMode == mode)
        {
            return;
        }

        bool wasLocked = (CursorMode & CursorMode.Locked) != 0;
        bool isLocked = (mode & CursorMode.Locked) != 0;

        SDL.SetWindowRelativeMouseMode(Handle, isLocked);
        SDL.SetWindowMouseGrab(Handle, (mode & CursorMode.Confined) != 0);

        if ((mode & CursorMode.Hidden) != 0)
        {
            SDL.HideCursor();
        }
        else
        {
            SDL.ShowCursor();
        }

        if (wasLocked && !isLocked)
        {
            SDL.WarpMouseInWindow(Handle, Resolution.W / 2.0f, Resolution.H / 2.0f);
        }

        CursorMode = mode;
    }

    public void SetVsyncMode(bool enabled)
    {
        IsVsyncOn = enabled; // This is more a renderer thing than windowing, but whatever
    }

    protected override unsafe void Dispose(bool disposing)
    {
        SDL.DestroyWindow(Handle);
    }

    private unsafe void Restore()
    {
        if (IsFullscreen)
        {
            SetFullscreen(false);
        }

        if (IsMinimized || IsMaximized)
        {
            SDL.RestoreWindow(Handle);
        }

        IsMinimized = false;
        IsMaximized = false;
    }

    private unsafe void Center()
    {
        uint current = SDL.GetDisplayForWindow(Handle);
        SetPosition((int)(SDL.SDL_WINDOWPOS_CENTERED_MASK | current), (int)(SDL.SDL_WINDOWPOS_CENTERED_MASK | current));
    }
}
