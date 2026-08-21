using Chisel.Framework;
using Chisel.Framework.Utilities;
using Chisel.Resource;
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
    Texture2D defaultSpecular;
    Texture2D defaultNormal;
    ISampler sampler;

    RenderTarget2D screenTexture;

    // Camera state
    Vector3 cameraPosition = new Vector3(0, 1.5f, 4f);
    float yaw = -MathUtilities.PiOverTwoF; // facing -Z toward the cube
    float pitch = 0f;
    const float MouseSensitivity = 0.0025f;
    const float MoveSpeed = 4f;
    const float FastMoveSpeed = 10f;

    SpriteBatch spriteBatch;

    Matrix4 projection;
    Matrix4 view;
    Vector3 forward;
    double elapsed;

    public TestGame() : base(GraphicsBackend.Auto, false)
    {
        Window.SetVsyncMode(false);
    }

    protected override unsafe void OnStartup()
    {
        base.OnStartup();

        modelShader = Content.Load<ShaderEffect>("Shaders/Model");
        modelShader.SetTechnique("Default");

        face = Content.Load<Texture2D>("Textures/test");
        grid = Content.Load<Texture2D>("Textures/devGrid");

        defaultSpecular = CreateFlatTexture(GraphicsDevice, 128, 128, 128, 0);
        defaultNormal = CreateFlatTexture(GraphicsDevice, 128, 128, 255, 255);

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
    }

    static Texture2D CreateFlatTexture(IGraphicsDevice device, byte r, byte g, byte b, byte a)
    {
        var tex = new Texture2D(device, 1, 1);
        tex.SetData(new byte[] { r, g, b, a });
        return tex;
    }

    void UpdateProjection()
    {
        float aspect = Window.Resolution.W / (float)Window.Resolution.H;
        projection = Matrix4.FromPerspectiveFov(
            70f * MathUtilities.Deg2RadF, aspect, 0.1f, 100f);
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
        bool looking = InputManager.IsInputHeld(Input.MouseLeft);
        if (looking)
        {
            Window.SetCursorMode(CursorMode.Locked | CursorMode.Hidden);
            Vector2 md = InputManager.MouseDelta;
            yaw += md.X * MouseSensitivity;
            pitch -= md.Y * MouseSensitivity;
            pitch = pitch.Clamp(-MathUtilities.PiOverTwoF + 0.01f, MathUtilities.PiOverTwoF - 0.01f);
        }
        else
        {
            Window.SetCursorMode(CursorMode.Normal);
        }

        forward = new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Sin(yaw)).Normalize();

        Vector3 right = forward.CrossProduct(Vector3.UnitY).Normalize();

        float speed = InputManager.IsInputHeld(Input.KeyLShift) ? FastMoveSpeed : MoveSpeed;
        float move = speed * (float)delta;

        if (InputManager.IsInputHeld(Input.KeyW)) cameraPosition += forward * move;
        if (InputManager.IsInputHeld(Input.KeyS)) cameraPosition -= forward * move;
        if (InputManager.IsInputHeld(Input.KeyA)) cameraPosition -= right * move;
        if (InputManager.IsInputHeld(Input.KeyD)) cameraPosition += right * move;

        view = Matrix4.FromLookAt(cameraPosition, cameraPosition + forward, Vector3.UnitY);
    }

    protected override void OnDrawFrame(double delta)
    {
        elapsed += delta;

        screenTexture.Begin();

        GraphicsDevice.Clear(new Color(15, 15, 15));

        QuickDraw.SetShader(modelShader.CurrentTechnique);

        modelShader.Parameters["View"]?.SetValue(view);
        modelShader.Parameters["Projection"]?.SetValue(projection);
        modelShader.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        modelShader.Parameters["CameraForward"]?.SetValue(forward);
        modelShader.Parameters["Time"]?.SetValue((float)elapsed);
        modelShader.Parameters["ScreenSize"]?.SetValue(new Vector2(Window.Resolution.W, Window.Resolution.H));

        Vector3 sunDir = new Vector3(-0.4f, -1f, -0.3f).Normalize();
        modelShader.Parameters["SunDirection"]?.SetValue(sunDir);
        modelShader.Parameters["SunIntensity"]?.SetValue(0.6f);
        modelShader.Parameters["SunColor"]?.SetValue(new Vector4(1f, 0.95f, 0.85f, 1f));

        Vector3 lightPos = new Vector3(MathF.Cos(-(float)elapsed) * 8f, 2.5f, MathF.Sin(-(float)elapsed) * 8f);
        var positions = new[] { new Vector4(lightPos.X, lightPos.Y, lightPos.Z, 16) };
        var colors = new[] { new Vector4(1f, 0.85f, 0.6f, 1f) };
        var spotData = new[] { Vector4.Zero };

        modelShader.Parameters["RealtimeLightCount"]?.SetValue(1);
        modelShader.Parameters["RealtimeLightPositions"]?.SetValue(new ReadOnlySpan<Vector4>(positions));
        modelShader.Parameters["RealtimeLightColors"]?.SetValue(new ReadOnlySpan<Vector4>(colors));
        modelShader.Parameters["RealtimeLightSpotData"]?.SetValue(new ReadOnlySpan<Vector4>(spotData));

        modelShader.Parameters["DiffuseSampler"].SetValue(sampler);
        modelShader.Parameters["SpecularSampler"]?.SetValue(sampler);
        modelShader.Parameters["NormalSampler"]?.SetValue(sampler);

        foreach (var obj in sceneObjects)
        {
            Matrix4 world = obj.GetWorld(elapsed);
            Matrix4 worldInverseTranspose = world.Invert().Transpose();

            modelShader.Parameters["World"]?.SetValue(world);
            modelShader.Parameters["WorldInverseTranspose"]?.SetValue(worldInverseTranspose);

            modelShader.Parameters["Shininess"]?.SetValue(obj.Shininess);
            modelShader.Parameters["Transparent"]?.SetValue(obj.Transparent ? 1 : 0);

            modelShader.Parameters["DiffuseTexture"].SetValue(obj.Texture);
            modelShader.Parameters["SpecularTexture"]?.SetValue(defaultSpecular);
            modelShader.Parameters["NormalTexture"]?.SetValue(defaultNormal);

            QuickDraw.BindVertexBuffer(obj.Mesh.Vertices);
            QuickDraw.BindIndexBuffer(obj.Mesh.Indices);
            QuickDraw.DrawIndexed((uint)obj.Mesh.IndexCount);
        }

        screenTexture.End();

        spriteBatch.Begin(Matrix4.FromOrthographic(0, Window.Resolution.W, Window.Resolution.H, 0, 0, 1));
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
        defaultSpecular.Dispose();
        defaultNormal.Dispose();
    }
    void BuildScene()
    {
        // ---------------------------------------------------------------------
        // Meshes
        // ---------------------------------------------------------------------

        cubeMesh = new MeshBuffers(
            GraphicsDevice,
            PrimitiveBuilder.CreateCube(1f, new Vector2(1, 1)));

        sphereMesh = new MeshBuffers(
            GraphicsDevice,
            PrimitiveBuilder.CreateSphere(0.5f, 16, 12));

        groundMesh = new MeshBuffers(
            GraphicsDevice,
            PrimitiveBuilder.CreatePlane(40f, 40f, new Vector2(16, 16)));


        // ---------------------------------------------------------------------
        // Ground
        // ---------------------------------------------------------------------

        sceneObjects.Add(new SceneObject
        {
            Mesh = groundMesh,
            Texture = grid,
            Position = new Vector3(0, -1.5f, 0),
            Scale = Vector3.One
        });


        // ---------------------------------------------------------------------
        // Central rotating structure
        // ---------------------------------------------------------------------

        sceneObjects.Add(new SceneObject
        {
            Mesh = cubeMesh,
            Texture = face,
            Position = Vector3.Zero,
            Scale = new Vector3(1.5f),
            SpinSpeed = 0.35f
        });

        // Large central spheres
        for (int i = 0; i < 8; i++)
        {
            float angle = i / 8f * MathUtilities.TauF;

            float radius = 3.2f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = sphereMesh,
                Texture = grid,

                Position = new Vector3(
                    MathF.Cos(angle) * radius,
                    0.5f + MathF.Sin(angle * 2f) * 0.5f,
                    MathF.Sin(angle) * radius),

                Scale = new Vector3(0.35f),

                Orbits = true,
                OrbitRadius = radius,
                OrbitSpeed = 0.35f + i * 0.03f,
                OrbitPhase = angle,

                BobAmplitude = 0.3f + i * 0.03f,
                BobSpeed = 1f + i * 0.1f,

                SpinSpeed = 1f + i * 0.15f
            });
        }


        // ---------------------------------------------------------------------
        // Orbital ring #1
        // ---------------------------------------------------------------------

        const int Ring1Count = 150;

        for (int i = 0; i < Ring1Count; i++)
        {
            float phase = i / (float)Ring1Count * MathUtilities.TauF;

            float radius = 5.5f + MathF.Sin(i * 1.37f) * 0.35f;

            float height =
                MathF.Sin(i * 2.31f) * 0.8f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = cubeMesh,
                Texture = i % 5 == 0 ? face : grid,

                Position = new Vector3(
                    MathF.Cos(phase) * radius,
                    height,
                    MathF.Sin(phase) * radius),

                Scale = new Vector3(
                    0.12f + (i % 4) * 0.04f),

                Orbits = true,
                OrbitRadius = radius,
                OrbitSpeed = 0.45f + (i % 7) * 0.025f,
                OrbitPhase = phase,

                BobAmplitude = 0.15f + (i % 5) * 0.08f,
                BobSpeed = 0.8f + (i % 9) * 0.15f,

                SpinSpeed = -2f + (i % 11) * 0.4f
            });
        }


        // ---------------------------------------------------------------------
        // Orbital ring #2
        // ---------------------------------------------------------------------

        const int Ring2Count = 220;

        for (int i = 0; i < Ring2Count; i++)
        {
            float phase = i / (float)Ring2Count * MathUtilities.TauF;

            float radius =
                8f +
                MathF.Sin(i * 0.71f) * 0.8f +
                MathF.Cos(i * 1.17f) * 0.3f;

            float height =
                MathF.Sin(i * 0.37f) * 1.8f;

            float size =
                0.10f +
                (i % 6) * 0.025f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = sphereMesh,
                Texture = grid,

                Position = new Vector3(
                    MathF.Cos(phase) * radius,
                    height,
                    MathF.Sin(phase) * radius),

                Scale = new Vector3(size),

                Orbits = true,
                OrbitRadius = radius,
                OrbitSpeed =
                    0.15f +
                    (i % 13) * 0.018f,

                OrbitPhase = phase,

                BobAmplitude =
                    0.25f +
                    (i % 7) * 0.08f,

                BobSpeed =
                    0.5f +
                    (i % 11) * 0.13f,

                SpinSpeed =
                    0.5f +
                    (i % 9) * 0.25f
            });
        }


        // ---------------------------------------------------------------------
        // Orbital ring #3
        // ---------------------------------------------------------------------

        const int Ring3Count = 280;

        for (int i = 0; i < Ring3Count; i++)
        {
            float phase =
                i / (float)Ring3Count * MathUtilities.TauF;

            float radius =
                11f +
                MathF.Sin(i * 1.19f) * 1.2f;

            float height =
                MathF.Sin(i * 0.53f) * 3f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = cubeMesh,
                Texture = i % 9 == 0 ? face : grid,

                Position = new Vector3(
                    MathF.Cos(phase) * radius,
                    height,
                    MathF.Sin(phase) * radius),

                Scale = new Vector3(
                    0.08f +
                    (i % 5) * 0.035f),

                Orbits = true,
                OrbitRadius = radius,

                OrbitSpeed =
                    0.25f +
                    (i % 17) * 0.025f,

                OrbitPhase = phase,

                BobAmplitude =
                    0.2f +
                    (i % 8) * 0.12f,

                BobSpeed =
                    0.6f +
                    (i % 10) * 0.17f,

                SpinSpeed =
                    -4f +
                    (i % 15) * 0.6f
            });
        }

        System.Random random = new System.Random(1337);

        const int FloatingCount = 450;

        for (int i = 0; i < FloatingCount; i++)
        {
            float x = (float)(random.NextDouble() * 44.0 - 22.0);
            float y = (float)(random.NextDouble() * 14.0 - 1.0);
            float z = (float)(random.NextDouble() * 44.0 - 22.0);

            float size =
                0.5f +
                (float)random.NextDouble() * 0.20f;

            float orbitRadius =
                MathF.Sqrt(x * x + z * z);

            float phase =
                MathF.Atan2(z, x);

            if (orbitRadius < 2f)
                orbitRadius = 2f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = i % 3 == 0 ? sphereMesh : cubeMesh,
                Texture = i % 17 == 0 ? face : grid,

                Position = new Vector3(x, y, z),

                Scale = new Vector3(size),

                Orbits = true,
                OrbitRadius = orbitRadius,

                OrbitSpeed =
                    0.02f +
                    (float)random.NextDouble() * 0.12f,

                OrbitPhase = phase,

                BobAmplitude =
                    0.1f +
                    (float)random.NextDouble() * 0.7f,

                BobSpeed =
                    0.3f +
                    (float)random.NextDouble() * 2f,

                SpinSpeed =
                    -3f +
                    (float)random.NextDouble() * 6f
            });
        }


        // ---------------------------------------------------------------------
        // Inner satellite swarm
        // ---------------------------------------------------------------------

        const int SatelliteCount = 130;

        for (int i = 0; i < SatelliteCount; i++)
        {
            float phase =
                i / (float)SatelliteCount * MathUtilities.TauF;

            float radius =
                2f + (i % 6) * 0.25f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = cubeMesh,
                Texture = i % 4 == 0 ? face : grid,

                Position = new Vector3(
                    MathF.Cos(phase) * radius,
                    0,
                    MathF.Sin(phase) * radius),

                Scale = new Vector3(
                    0.08f + (i % 3) * 0.025f),

                Orbits = true,
                OrbitRadius = radius,

                OrbitSpeed =
                    1.0f +
                    (i % 10) * 0.08f,

                OrbitPhase = phase,

                BobAmplitude =
                    0.1f +
                    (i % 5) * 0.08f,

                BobSpeed =
                    1.5f +
                    (i % 7) * 0.25f,

                SpinSpeed =
                    -5f +
                    (i % 9) * 1.1f
            });
        }


        // ---------------------------------------------------------------------
        // Vertical towers of moving objects
        // ---------------------------------------------------------------------

        const int TowerCount = 18;
        const int ObjectsPerTower = 20;

        for (int tower = 0; tower < TowerCount; tower++)
        {
            float angle =
                tower / (float)TowerCount * MathUtilities.TauF;

            float radius = 15f;

            float towerX =
                MathF.Cos(angle) * radius;

            float towerZ =
                MathF.Sin(angle) * radius;

            for (int y = 0; y < ObjectsPerTower; y++)
            {
                float height =
                    -0.5f + y * 0.8f;

                sceneObjects.Add(new SceneObject
                {
                    Mesh = y % 2 == 0 ? cubeMesh : sphereMesh,
                    Texture = y % 5 == 0 ? face : grid,

                    Position = new Vector3(
                        towerX,
                        height,
                        towerZ),

                    Scale = new Vector3(
                        0.12f + (y % 3) * 0.04f),

                    Orbits = true,

                    OrbitRadius =
                        radius +
                        MathF.Sin(y * 0.7f) * 0.8f,

                    OrbitSpeed =
                        0.05f +
                        y * 0.008f,

                    OrbitPhase =
                        angle +
                        y * 0.15f,

                    BobAmplitude =
                        0.15f +
                        y * 0.02f,

                    BobSpeed =
                        0.7f +
                        y * 0.1f,

                    SpinSpeed =
                        1f +
                        y * 0.25f
                });
            }
        }


        // ---------------------------------------------------------------------
        // Far background spheres
        // ---------------------------------------------------------------------

        const int FarCount = 40;

        for (int i = 0; i < FarCount; i++)
        {
            float phase =
                i / (float)FarCount * MathUtilities.TauF;

            float radius = 18f + (i % 3) * 2f;

            sceneObjects.Add(new SceneObject
            {
                Mesh = sphereMesh,
                Texture = face,

                Position = new Vector3(
                    MathF.Cos(phase) * radius,
                    3f + MathF.Sin(phase * 3f),
                    MathF.Sin(phase) * radius),

                Scale = new Vector3(
                    0.4f + (i % 4) * 0.15f),

                Orbits = true,
                OrbitRadius = radius,

                OrbitSpeed =
                    0.025f +
                    i * 0.002f,

                OrbitPhase = phase,

                BobAmplitude = 1f,
                BobSpeed = 0.2f + i * 0.03f,

                SpinSpeed =
                    0.2f +
                    i * 0.05f
            });
        }


        // ---------------------------------------------------------------------
        // Spiral galaxy arms
        // ---------------------------------------------------------------------

        const int SpiralArms = 4;
        const int PerArm = 150;

        for (int arm = 0; arm < SpiralArms; arm++)
        {
            float armOffset = arm / (float)SpiralArms * MathUtilities.TauF;

            for (int i = 0; i < PerArm; i++)
            {
                float t = i / (float)PerArm;

                float radius = 3f + t * 22f;

                float phase = armOffset + t * MathUtilities.TauF * 2.5f;

                float height = MathF.Sin(t * MathUtilities.TauF * 3f + arm) * 1.5f;

                sceneObjects.Add(new SceneObject
                {
                    Mesh = i % 4 == 0 ? sphereMesh : cubeMesh,
                    Texture = i % 6 == 0 ? face : grid,

                    Position = new Vector3(
                        MathF.Cos(phase) * radius,
                        height,
                        MathF.Sin(phase) * radius),

                    Scale = new Vector3(0.06f + (1f - t) * 0.18f),

                    Orbits = true,
                    OrbitRadius = radius,
                    OrbitSpeed = 0.08f + (1f - t) * 0.2f,
                    OrbitPhase = phase,

                    BobAmplitude = 0.2f + t * 0.6f,
                    BobSpeed = 0.4f + (i % 7) * 0.1f,

                    SpinSpeed = -2f + (i % 13) * 0.35f
                });
            }
        }


        // ---------------------------------------------------------------------
        // Double helix
        // ---------------------------------------------------------------------

        const int HelixSegments = 220;
        const float HelixHeight = 32f;
        const float HelixRadius = 6.5f;
        const float HelixTurns = 6f;

        for (int strand = 0; strand < 2; strand++)
        {
            float strandOffset = strand * MathUtilities.PiF;

            for (int i = 0; i < HelixSegments; i++)
            {
                float t = i / (float)HelixSegments;

                float phase = strandOffset + t * MathUtilities.TauF * HelixTurns;

                float height = -HelixHeight * 0.5f + t * HelixHeight;

                sceneObjects.Add(new SceneObject
                {
                    Mesh = cubeMesh,
                    Texture = strand == 0 ? face : grid,

                    Position = new Vector3(
                        MathF.Cos(phase) * HelixRadius,
                        height,
                        MathF.Sin(phase) * HelixRadius),

                    Scale = new Vector3(0.14f),

                    Orbits = true,
                    OrbitRadius = HelixRadius,
                    OrbitSpeed = 0.3f,
                    OrbitPhase = phase,

                    BobAmplitude = 0.05f,
                    BobSpeed = 2f + strand,

                    SpinSpeed = 3f * (strand == 0 ? 1f : -1f)
                });
            }
        }

        Console.WriteLine($"Created {sceneObjects.Count} scene objects.");
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
