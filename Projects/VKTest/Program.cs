using System;
using Chisel.Framework;

namespace VKTest;

public class TestGame : Game
{

    public TestGame() : base(GraphicsBackend.Vulkan, true)
    {

    }

    protected override unsafe void OnStartup()
    {
        base.OnStartup();
    }

    protected override void OnFrameUpdate(double delta)
    {
        base.OnFrameUpdate(delta);
    }

    protected override void OnDrawFrame(double delta)
    {
        base.OnDrawFrame(delta);

        GraphicsDevice.BeginFrame();
        GraphicsDevice.Clear(Color.Aqua, 1f, 0, GraphicsClearFlags.Color | GraphicsClearFlags.Depth);
        GraphicsDevice.EndFrame();
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
