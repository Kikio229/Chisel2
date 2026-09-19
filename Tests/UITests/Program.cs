using Chisel.Framework;
using Chisel.Framework.UI;
using Chisel.Resource;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
struct VertexPositionColor
{
    [Vertex(0)] public Vector3 Position;
    [Vertex(1)] public Vector3 Color;

    public VertexPositionColor(Vector3 position, Vector3 color)
    {
        Position = position;
        Color = color;
    }
}

public class TestGame : Game
{
    // Cube resources
    VertexBuffer<SimpleModelVertex> cubeVertices;
    IndexBuffer cubeIndices;
    ShaderEffect cubeShader;

    Texture2D texture;
    Texture2D testUItex;
    ISampler sampler;

    RenderTarget2D screenTexture;

    // Camera state
    Vector3 cameraPosition = new Vector3(0, 1.5f, 4f);
    float yaw = -float.Pi/2; // facing -Z toward the cube
    float pitch = 0f;
    const float MouseSensitivity = 0.0025f;
    const float MoveSpeed = 4f;
    const float FastMoveSpeed = 10f;

    SpriteBatch spriteBatch;

    Matrix projection;
    Matrix view;
    Vector3 forward;
    double elapsed;

    private UIManager uiManager;
    private UIContextMenu testContextMenu;

    public TestGame() : base(GraphicsBackend.OpenGL, false)
    {
        Window.SetVsyncMode(false);
    }

    protected override unsafe void OnStartup()
    {
        base.OnStartup();

        BuildCube();

        cubeShader = Content.Load<ShaderEffect>("Shaders/SimpleModel");
        cubeShader.SetTechnique("Default");

        texture = Content.Load<Texture2D>("Textures/test");
        testUItex = Content.Load<Texture2D>("Textures/UIatlas");
        sampler = GraphicsDevice.CreateSampler(new SamplerDescription
        {
            FilterMode = SamplerFilterMode.Bilinear | SamplerFilterMode.MipmapBilinear,
            WrapMode = SamplerWrapMode.Repeat,
        });

        uiManager = new UIManager();

        //imgui = new ImGuiRenderer(this);
        //imgui.RebuildFontAtlas();

        QuickDraw.SetTopology(GraphicsTopology.TriangleList);
        QuickDraw.SetDepthMode(GraphicsDepthMode.Less);
        QuickDraw.SetBlendMode(GraphicsBlendMode.Opaque);
        QuickDraw.SetCullMode(GraphicsCullMode.Back);
        QuickDraw.SetFillMode(GraphicsFillMode.Solid);
        QuickDraw.SetDepthWrite(true);

        spriteBatch = new SpriteBatch(GraphicsDevice, Content);

        screenTexture = new RenderTarget2D(GraphicsDevice, Window.Resolution.W, Window.Resolution.H,
            ImageFormat.R32G32B32A32Float, ImageFormat.D24UNormS8UInt, 4);

        uiManager.SetSize(Window.Resolution.W, Window.Resolution.H);

        BuildTestUI();
        UpdateProjection();
    }

    private void BuildTestUI()
    {
        var dockRoot = new UIDockPanel(default)
        {
            Anchor = UIAnchor.FillH | UIAnchor.FillV,
            HalfSizeOffset = new Vector2(0, 0),
            CenterOffset = new Vector2(0, 0),
            Tint = new Color(20, 20, 20),
        };

        var menuBar = new UIMenuBar(default)
        {
            Tint = new Color(15, 15, 15),
        };

        menuBar.SetMenus(new List<UIMenuBarMenu>
        {
            new UIMenuBarMenu("File", new List<UIMenuItem>
            {
                new UIMenuItem("New", () => Console.WriteLine("New clicked")),
                new UIMenuItem("Open", () => Console.WriteLine("Open clicked")),
                new UIMenuItem("Save", () => Console.WriteLine("Save clicked")),
            }),
            new UIMenuBarMenu("Edit", new List<UIMenuItem>
            {
                new UIMenuItem("Undo", () => Console.WriteLine("Undo clicked")),
                new UIMenuItem("Redo", () => Console.WriteLine("Redo clicked")),
            }),
        });

        var splitView = new UIFourWaySplitView(default)
        {
            Tint = new Color(20, 20, 20),
        };

        var topLeftLabel = new UITextBlock(default)
        {
            Anchor = UIAnchor.Top | UIAnchor.Left,
            Text = "Text input test",
            FontSize = 20,
            CenterOffset = new Vector2(8, 8),
        };
        splitView.TopLeft.Tint = new Color(45, 55, 75);
        splitView.TopLeft.AddChild(topLeftLabel);

        var textInput = new UITextInput(default)
        {
            Anchor = UIAnchor.Top | UIAnchor.Left,
            HalfSizeOffset = new Vector2(100, 16),
            CenterOffset = new Vector2(108, 44),
            Placeholder = "Type here...",
            Tint = new Color(35, 35, 35),
        };
        splitView.TopLeft.AddChild(textInput);

        var comboBox = new UIComboBox(default)
        {
            Anchor = UIAnchor.Center,
            HalfSizeOffset = new Vector2(80, 16),
            CenterOffset = new Vector2(0, 0),
            Items = new List<string> { "Option A", "Option B", "Option C" },
            SelectedIndex = 0,
        };
        splitView.TopRight.Tint = new Color(45, 70, 50);
        splitView.TopRight.AddChild(comboBox);

        var checkButton = new UICheckButton(default)
        {
            Anchor = UIAnchor.Center,
            HalfSizeOffset = new Vector2(16, 16),
            CenterOffset = new Vector2(-60, 0),
            Tint = new Color(35, 35, 35),
        };
        var checkLabel = new UITextBlock(default)
        {
            Anchor = UIAnchor.Center,
            Text = "Enable thing",
            FontSize = 20,
            CenterOffset = new Vector2(30, 0),
        };
        splitView.BottomLeft.Tint = new Color(75, 50, 50);
        splitView.BottomLeft.AddChild(checkButton);
        splitView.BottomLeft.AddChild(checkLabel);

        testContextMenu = new UIContextMenu(default);

        var contextItems = new List<UIMenuItem>
        {
            new UIMenuItem("Delete", () => Console.WriteLine("Delete clicked")),
            new UIMenuItem("Rename", () => Console.WriteLine("Rename clicked")),
            new UIMenuItem("Properties", () => Console.WriteLine("Properties clicked")),
        };

        var contextTrigger = new UITestContextTrigger(default, testContextMenu, contextItems)
        {
            Anchor = UIAnchor.FillH | UIAnchor.FillV,
            Tint = new Color(60, 60, 40),
        };

        var rightClickHint = new UITextBlock(default)
        {
            Anchor = UIAnchor.Top | UIAnchor.Left,
            Text = "Right click for menu",
            FontSize = 20,
            CenterOffset = new Vector2(8, 8),
        };

        splitView.BottomRight.AddChild(contextTrigger);
        contextTrigger.AddChild(rightClickHint);
        contextTrigger.AddChildOnTop(testContextMenu);

        dockRoot.AddDocked(menuBar, DockSide.Top, 32);
        dockRoot.AddFill(splitView);

        uiManager.AddToRoot(dockRoot);
    }

    void UpdateProjection()
    {
        float aspect = Window.Resolution.W / (float)Window.Resolution.H;
        projection = Matrix.FromPerspectiveFov(
            float.DegreesToRadians(70f), aspect, 0.1f, 100f);
    }
    void BuildCube()
    {
        Vector4 red = new(1, 0, 0, 1);
        Vector4 green = new(0, 1, 0, 1);
        Vector4 blue = new(0, 0, 1, 1);
        Vector4 yellow = new(1, 1, 0, 1);
        Vector4 cyan = new(0, 1, 1, 1);
        Vector4 magenta = new(1, 0, 1, 1);
        Vector4 norm = new(1, 1, 1, 1);

        Vector3 nx = new(1, 0, 0);
        Vector3 nnx = new(-1, 0, 0);
        Vector3 ny = new(0, 1, 0);
        Vector3 nny = new(0, -1, 0);
        Vector3 nz = new(0, 0, 1);
        Vector3 nnz = new(0, 0, -1);

        Vector2 uv00 = new(0, 0), uv10 = new(1, 0), uv11 = new(1, 1), uv01 = new(0, 1);

        float h = 0.5f;
        var verts = new SimpleModelVertex[]
        {
            // +X (red)
            new() { Position = new(h,-h,-h), Normal = nx, Color = norm, TexCoord = uv00 },
            new() { Position = new(h, h,-h), Normal = nx, Color = norm, TexCoord = uv10 },
            new() { Position = new(h, h, h), Normal = nx, Color = norm, TexCoord = uv11 },
            new() { Position = new(h,-h, h), Normal = nx, Color = norm, TexCoord = uv01 },

            // -X (green)
            new() { Position = new(-h,-h, h), Normal = nnx, Color = norm, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = nnx, Color = norm, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnx, Color = norm, TexCoord = uv11 },
            new() { Position = new(-h,-h,-h), Normal = nnx, Color = norm, TexCoord = uv01 },

            // +Y (blue)
            new() { Position = new(-h, h,-h), Normal = ny, Color = norm, TexCoord = uv00 },
            new() { Position = new(-h, h, h), Normal = ny, Color = norm, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = ny, Color = norm, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = ny, Color = norm, TexCoord = uv01 },

            // -Y (yellow)
            new() { Position = new(-h,-h, h), Normal = nny, Color = norm, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nny, Color = norm, TexCoord = uv10 },
            new() { Position = new( h,-h,-h), Normal = nny, Color = norm, TexCoord = uv11 },
            new() { Position = new( h,-h, h), Normal = nny, Color = norm, TexCoord = uv01 },

            // +Z (cyan)
            new() { Position = new(-h,-h, h), Normal = nz, Color = norm, TexCoord = uv00 },
            new() { Position = new( h,-h, h), Normal = nz, Color = norm, TexCoord = uv10 },
            new() { Position = new( h, h, h), Normal = nz, Color = norm, TexCoord = uv11 },
            new() { Position = new(-h, h, h), Normal = nz, Color = norm, TexCoord = uv01 },

            // -Z (magenta)
            new() { Position = new( h,-h,-h), Normal = nnz, Color = norm, TexCoord = uv00 },
            new() { Position = new(-h,-h,-h), Normal = nnz, Color = norm, TexCoord = uv10 },
            new() { Position = new(-h, h,-h), Normal = nnz, Color = norm, TexCoord = uv11 },
            new() { Position = new( h, h,-h), Normal = nnz, Color = norm, TexCoord = uv01 },
        };

        var indices = new uint[6 * 6];
        for (uint face = 0; face < 6; face++)
        {
            uint b = face * 4;
            uint o = face * 6;
            indices[o + 0] = b + 0; indices[o + 1] = b + 1; indices[o + 2] = b + 2;
            indices[o + 3] = b + 2; indices[o + 4] = b + 3; indices[o + 5] = b + 0;
        }

        cubeVertices = new VertexBuffer<SimpleModelVertex>(GraphicsDevice, verts.Length);
        cubeVertices.SetData(verts);

        cubeIndices = new IndexBuffer(GraphicsDevice, indices.Length);
        cubeIndices.SetData(indices);
    }
    protected override void OnWindowResize(int width, int height)
    {
        base.OnWindowResize(width, height);

        GraphicsDevice.SetViewport(new(0, 0), new(width, height));
        GraphicsDevice.SetScissor(new(0, 0), new(width, height));

        screenTexture.Resize(Window.Resolution.W, Window.Resolution.H);

        uiManager.SetSize(width,height);

        UpdateProjection();
    }
    protected override void OnFrameUpdate(double delta)
    {
        elapsed += delta;

        bool looking = InputManager.IsInputHeld(Input.MouseRight);
        if (looking)
        {
            Window.SetCursorMode(CursorMode.Locked | CursorMode.Hidden);
            Vector2 md = InputManager.MouseDelta;
            yaw += md.X * MouseSensitivity;
            pitch -= md.Y * MouseSensitivity;
            pitch = float.Clamp(pitch, -(float.Pi / 2) + 0.01f, (float.Pi / 2) - 0.01f);
        }
        else
        {
            Window.SetCursorMode(CursorMode.Normal);
        }

        forward = (new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Sin(yaw))).Normalize();

        Vector3 right = (forward.CrossProduct(Vector3.UnitY)).Normalize();

        float speed = InputManager.IsInputHeld(Input.KeyLShift) ? FastMoveSpeed : MoveSpeed;
        float move = speed * (float)delta;

        if (InputManager.IsInputHeld(Input.KeyW)) cameraPosition += forward * move;
        if (InputManager.IsInputHeld(Input.KeyS)) cameraPosition -= forward * move;
        if (InputManager.IsInputHeld(Input.KeyA)) cameraPosition -= right * move;
        if (InputManager.IsInputHeld(Input.KeyD)) cameraPosition += right * move;

        view = Matrix.FromLookAt(cameraPosition, cameraPosition + forward, Vector3.UnitY);

        uiManager.FrameUpdate((float)delta);
    }

    protected override void OnDrawFrame(double delta)
    {
        screenTexture.Begin();

        GraphicsDevice.Clear(new Color(15, 15, 15));

        QuickDraw.SetShader(cubeShader.CurrentTechnique);

        cubeShader.Parameters["View"]?.SetValue(view);
        cubeShader.Parameters["Projection"]?.SetValue(projection);
        cubeShader.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        cubeShader.Parameters["CameraForward"]?.SetValue(forward);
        cubeShader.Parameters["Time"]?.SetValue((float)elapsed);
        cubeShader.Parameters["ScreenSize"]?.SetValue(new Vector2(Window.Resolution.W, Window.Resolution.H));

        Matrix world = Matrix.FromRotationY((float)elapsed * 0.5f);
        cubeShader.Parameters["World"]?.SetValue(world);

        // One warm point light orbiting the cube.
        Vector3 lightPos = new Vector3(MathF.Cos((float)elapsed) * 3f, 1.5f, MathF.Sin((float)elapsed) * 3f);
        var positions = new[] { new Vector4(lightPos.X, lightPos.Y, lightPos.Z, 6f) };            // xyz = pos, w = range
        var colors = new[] { new Vector4(1f, 0.85f, 0.6f, 2.5f) };   // rgb = color, a = intensity
        var spotData = new[] { Vector4.Zero };                          // w = 0 -> omni light

        cubeShader.Parameters["RealtimeLightCount"]?.SetValue(1);
        cubeShader.Parameters["RealtimeLightPositions"]?.SetValue(new ReadOnlySpan<Vector4>(positions));
        cubeShader.Parameters["RealtimeLightColors"]?.SetValue(new ReadOnlySpan<Vector4>(colors));
        cubeShader.Parameters["RealtimeLightSpotData"]?.SetValue(new ReadOnlySpan<Vector4>(spotData));

        cubeShader.Parameters["DiffuseTexture"].SetValue(texture);
        cubeShader.Parameters["DiffuseSampler"].SetValue(sampler);

        QuickDraw.BindVertexBuffer(cubeVertices);
        QuickDraw.BindIndexBuffer(cubeIndices);
        QuickDraw.DrawIndexed((uint)cubeIndices.Count);

        screenTexture.End();

        spriteBatch.Begin(Matrix.FromOrthographic(0, Window.Resolution.W, Window.Resolution.H, 0, 0, 1));

        spriteBatch.Draw(screenTexture, Vector2.Zero, new(Window.Resolution.W, Window.Resolution.H), Color.White);

        spriteBatch.End();

        uiManager.FrameRender((float)delta, spriteBatch, testUItex);
        GraphicsDevice.SetScissorEnabled(false);
    }
    protected override void OnShutdown()
    {
        base.OnShutdown();
        cubeVertices.Dispose();
        cubeIndices.Dispose();
        screenTexture.Dispose();
        spriteBatch.Dispose();
    }
}
public class UITestContextTrigger : UIPanel
{
    private readonly UIContextMenu contextMenu;
    private readonly List<UIMenuItem> items;

    public UITestContextTrigger(UILayoutOptions options, UIContextMenu contextMenu, List<UIMenuItem> items) : base(options)
    {
        this.contextMenu = contextMenu;
        this.items = items;
    }

    public override void OnSecondaryClicked()
    {
        contextMenu.Show(InputManager.MousePosition, items);
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
