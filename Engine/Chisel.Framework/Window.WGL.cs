using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

// -------------------------------------------------------------------------
// This is a custom windows openGL context setter thingie, because apparently
// (SDL bug #7263) SDL doesnt always create the windows context properly.
// -------------------------------------------------------------------------

#if WINDOWS
[StructLayout(LayoutKind.Sequential)]
struct PIXELFORMATDESCRIPTOR
{
    public byte nSize, nVersion;
    public uint dwFlags;
    public byte iPixelType;
    public byte cColorBits;
    public byte cRedBits, cRedShift;
    public byte cGreenBits, cGreenShift;
    public byte cBlueBits, cBlueShift;
    public byte cAlphaBits, cAlphaShift;
    public byte cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits;
    public byte cDepthBits, cStencilBits;
    public byte cAuxBuffers;
    public byte iLayerType;
    public byte bReserved;
    public uint dwMajorMin, dwMinorMin;
}

static class NativeWGL
{
    [DllImport("opengl32.dll")] public static extern IntPtr wglCreateContext(IntPtr hdc);
    [DllImport("opengl32.dll")] public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
    [DllImport("opengl32.dll")] public static extern bool wglDeleteContext(IntPtr hglrc);
    [DllImport("opengl32.dll")] public static extern IntPtr wglGetProcAddress(string name);
    [DllImport("gdi32.dll")] public static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR pfd);
    [DllImport("gdi32.dll")] public static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR pfd);
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate IntPtr WGLCreateContextAttribsARB(IntPtr hdc, IntPtr shareContext, int[] attribs);
}
#endif