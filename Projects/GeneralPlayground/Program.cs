using Chisel.Framework;
using Chisel.Framework.Utilities;
using Chisel.Resource;
using Microsoft.Xna.Framework;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class TestGame : Game
{
    MeshBuffers cubeMesh;
    MeshBuffers groundMesh;
    MeshBuffers sphereMesh;

    List<SceneObject> sceneObjects = new();

    ShaderEffect modelShader;

    Texture2D face;
    Texture2D grid;
    ISampler sampler;

    RenderTarget2D screenTexture;

    // Camera state
    Vector3 cameraPosition = new Vector3(0, 1.5f, 4f);
    float yaw = -MathHelper.PiOver2; // facing -Z toward the cube
    float pitch = 0f;
    const float MouseSensitivity = 0.0025f;
    const float MoveSpeed = 4f;
    const float FastMoveSpeed = 10f;

    SpriteBatch spriteBatch;

    Matrix projection;
    Matrix view;
    Vector3 forward;
    double elapsed;

    public TestGame() : base(GraphicsBackend.OpenGL46, true)
    {
        Window.SetTickMode(false);
        Window.SetVsyncMode(false);
    }

    protected override unsafe void OnStartup()
    {
        base.OnStartup();

        modelShader = Content.Load<ShaderEffect>("Shaders/SimpleModel");
        modelShader.SetTechnique("Default");

        face = Content.Load<Texture2D>("Textures/test");
        grid = Content.Load<Texture2D>("Textures/devGrid");
        sampler = GraphicsDevice.CreateSampler(new SamplerDescription
        {
            FilterMode = SamplerFilterMode.Bilinear | SamplerFilterMode.MipmapBilinear,
            WrapMode = SamplerWrapMode.Repeat,
        });

        BuildScene();

        QuickDraw.SetTopology(GraphicsTopology.TriangleList);
        QuickDraw.SetDepthMode(GraphicsDepthMode.Less);
        QuickDraw.SetBlendMode(GraphicsBlendMode.Opaque);
        QuickDraw.SetCullMode(GraphicsCullMode.Back);
        QuickDraw.SetFillMode(GraphicsFillMode.Solid);
        QuickDraw.SetDepthWrite(true);

        spriteBatch = new SpriteBatch(GraphicsDevice, Content);

        screenTexture = new RenderTarget2D(GraphicsDevice, Window.Resolution.W, Window.Resolution.H,
            ImageFormat.R32G32B32A32Float, ImageFormat.D24UNormS8UInt, 4);

        UpdateProjection();

        Window.SetTickMode(false);
    }

    void UpdateProjection()
    {
        float aspect = Window.Resolution.W / (float)Window.Resolution.H;
        projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(70f), aspect, 0.1f, 100f);
    }

    protected override void OnWindowResize(int width, int height)
    {
        base.OnWindowResize(width, height);

        GraphicsDevice.SetViewport(new(0, 0), new(width, height));

        screenTexture.Resize(Window.Resolution.W, Window.Resolution.H);

        UpdateProjection();
    }
    protected override void OnFrameUpdate(double delta)
    {
        elapsed += delta;

        bool looking = InputManager.IsInputHeld(Input.MouseLeft);
        if (looking)
        {
            Window.SetCursorMode(CursorMode.Locked | CursorMode.Hidden);
            Vector2 md = InputManager.MouseDelta;
            yaw += md.X * MouseSensitivity;
            pitch -= md.Y * MouseSensitivity;
            pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
        }
        else
        {
            Window.SetCursorMode(CursorMode.Normal);
        }

        forward = Vector3.Normalize(new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Sin(yaw)));

        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.Up));

        float speed = InputManager.IsInputHeld(Input.KeyLShift) ? FastMoveSpeed : MoveSpeed;
        float move = speed * (float)delta;

        if (InputManager.IsInputHeld(Input.KeyW)) cameraPosition += forward * move;
        if (InputManager.IsInputHeld(Input.KeyS)) cameraPosition -= forward * move;
        if (InputManager.IsInputHeld(Input.KeyA)) cameraPosition -= right * move;
        if (InputManager.IsInputHeld(Input.KeyD)) cameraPosition += right * move;

        view = Matrix.CreateLookAt(cameraPosition, cameraPosition + forward, Vector3.Up);
    }
    protected override void OnDrawFrame(double delta)
    {
        screenTexture.Begin();

        GraphicsDevice.Clear(new Color(15, 15, 15));

        QuickDraw.SetShader(modelShader.CurrentTechnique);

        modelShader.Parameters["View"]?.SetValue(view);
        modelShader.Parameters["Projection"]?.SetValue(projection);
        modelShader.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        modelShader.Parameters["CameraForward"]?.SetValue(forward);
        modelShader.Parameters["Time"]?.SetValue((float)elapsed);
        modelShader.Parameters["ScreenSize"]?.SetValue(new Vector2(Window.Resolution.W, Window.Resolution.H));

        // One warm point light orbiting the whole scene.
        Vector3 lightPos = new Vector3(MathF.Cos((float)elapsed) * 4f, 2.5f, MathF.Sin((float)elapsed) * 4f);
        var positions = new[] { new Vector4(lightPos, 8f) };
        var colors = new[] { new Vector4(1f, 0.85f, 0.6f, 2f) };
        var spotData = new[] { Vector4.Zero };

        modelShader.Parameters["RealtimeLightCount"]?.SetValue(1);
        modelShader.Parameters["RealtimeLightPositions"]?.SetValue(new ReadOnlySpan<Vector4>(positions));
        modelShader.Parameters["RealtimeLightColors"]?.SetValue(new ReadOnlySpan<Vector4>(colors));
        modelShader.Parameters["RealtimeLightSpotData"]?.SetValue(new ReadOnlySpan<Vector4>(spotData));

        foreach (var obj in sceneObjects)
        {
            modelShader.Parameters["DiffuseTexture"].SetValue(obj.Texture);
            modelShader.Parameters["DiffuseSampler"].SetValue(sampler);
            modelShader.Parameters["World"]?.SetValue(obj.GetWorld(elapsed));

            QuickDraw.BindVertexBuffer(obj.Mesh.Vertices);
            QuickDraw.BindIndexBuffer(obj.Mesh.Indices);
            QuickDraw.DrawIndexed((uint)obj.Mesh.IndexCount);
        }

        screenTexture.End();

        spriteBatch.Begin(Matrix.CreateOrthographicOffCenter(0, Window.Resolution.W, Window.Resolution.H, 0, 0, 1));
        spriteBatch.Draw(screenTexture, Vector2.Zero, new(Window.Resolution.W, Window.Resolution.H), Color.White);
        spriteBatch.End();

        GraphicsDevice.Clear(default, 1f, 0, GraphicsClearFlags.Depth);
    }

    protected override void OnShutdown()
    {
        base.OnShutdown();
        cubeMesh.Dispose();
        sphereMesh.Dispose();
        groundMesh.Dispose();
        screenTexture.Dispose();
        spriteBatch.Dispose();
    }

    void BuildScene()
    {
        cubeMesh = new MeshBuffers(GraphicsDevice, PrimitiveBuilder.CreateCube(1f, Vector4.One*0.05f));
        sphereMesh = new MeshBuffers(GraphicsDevice, PrimitiveBuilder.CreateSphere(0.5f, 24, 16, Vector4.One * 0.05f));
        groundMesh = new MeshBuffers(GraphicsDevice, PrimitiveBuilder.CreatePlane(20f, 20f, Vector4.One * 0.05f, new Vector2(8, 8)));

        // ground
        sceneObjects.Add(new SceneObject { Mesh = groundMesh, Texture = grid, Position = new Vector3(0, -1f, 0) });

        // centerpiece cube
        sceneObjects.Add(new SceneObject { Mesh = cubeMesh, Texture = face, Position = Vector3.Zero, SpinSpeed = 0.5f });

        // three small orbiting cubes
        sceneObjects.Add(new SceneObject
        {
            Mesh = cubeMesh,
            Texture = grid,
            Scale = new Vector3(0.4f),
            Position = new Vector3(0, 0.5f, 0),
            Orbits = true,
            OrbitRadius = 2.5f,
            OrbitSpeed = 0.9f,
            OrbitPhase = 0f,
            SpinSpeed = 2f
        });
        sceneObjects.Add(new SceneObject
        {
            Mesh = cubeMesh,
            Texture = grid,
            Scale = new Vector3(0.4f),
            Position = new Vector3(0, 0.2f, 0),
            Orbits = true,
            OrbitRadius = 2.5f,
            OrbitSpeed = 0.9f,
            OrbitPhase = MathHelper.TwoPi / 3f,
            SpinSpeed = -2f
        });
        sceneObjects.Add(new SceneObject
        {
            Mesh = cubeMesh,
            Texture = grid,
            Scale = new Vector3(0.4f),
            Position = new Vector3(0, 0.8f, 0),
            Orbits = true,
            OrbitRadius = 2.5f,
            OrbitSpeed = 0.9f,
            OrbitPhase = MathHelper.TwoPi * 2f / 3f,
            SpinSpeed = 2f
        });

        // two bobbing spheres
        sceneObjects.Add(new SceneObject
        {
            Mesh = sphereMesh,
            Texture = grid,
            Position = new Vector3(-3.5f, 0, -1f),
            BobAmplitude = 0.3f,
            BobSpeed = 1.3f
        });
        sceneObjects.Add(new SceneObject
        {
            Mesh = sphereMesh,
            Texture = grid,
            Position = new Vector3(3.5f, 0, 1f),
            BobAmplitude = 0.3f,
            BobSpeed = 1.7f
        });
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
