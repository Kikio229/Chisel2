using Chisel.Framework;
using Chisel.Resource;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using Vortice.Win32.Graphics.Direct3D.Dxc;

namespace DXTest;

public class TestGame : Game
{
    private IImage[] _images;
    private IGraphicsState _state;
    private IShader _vertShader, _fragShader; // Refer to GL GD and GL test. You dont have to load them like this.
    private ShaderPass testShader; // Do this instead
    // Then, in your working/shaders directory, create your Test.HLSL (reference the sprite.hlsl in Resource.Builder)
    // it will automatically compile those shaders *for* you, and handle loading them via Content.Load<ShaderProgram>

    public TestGame() : base(GraphicsBackend.Direct3D12, true)
    {
    }

    protected override unsafe void OnStartup()
    {
        base.OnStartup();
        Window.SetTickMode(false);
    }

    protected override void OnTickUpdate(double delta)
    {
        base.OnTickUpdate(delta);
    }

    protected override void OnFrameUpdate(double delta)
    {
        base.OnFrameUpdate(delta);

        //GraphicsDevice.BeginFrame();
        //GraphicsDevice.EndFrame(); See below VVV
    }

    protected override void OnDrawFrame(double delta)
    {
        // draw here

        GraphicsDevice.Clear(Color.Red);
    }

    protected override void OnShutdown()
    {
        base.OnShutdown();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        TestGame game = new TestGame();
        game.Run(args);
    }
}