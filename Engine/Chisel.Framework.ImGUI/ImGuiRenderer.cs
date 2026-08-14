using Chisel.Framework;
using Chisel.Resource;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Chisel.Framework.ImGUI;

public class ImGuiRenderer
{
    Game game;
    IGraphicsDevice device;

    // Graphics
    ShaderPass shader;
    ShaderParameter projectionParam;
    ShaderParameter textureParam;
    ShaderParameter samplerParam;
    ISampler sampler;
    IGraphicsState pipelineState;

    VertexBuffer<ImGuiVertex> vertexBuffer;
    IndexBuffer indexBuffer;
    int vertexCapacity;
    int indexCapacity;

    // Scratch arrays reused across frames to avoid per-frame allocation churn
    ImGuiVertex[] vertexScratch = Array.Empty<ImGuiVertex>();
    uint[] indexScratch = Array.Empty<uint>();

    // Textures
    Dictionary<nint, Texture2D> loadedTextures = new Dictionary<nint, Texture2D>();
    int textureId;
    nint? fontTextureId;

    // Input
    static readonly Input[] allInputs = (Input[])Enum.GetValues(typeof(Input));

    public ImGuiRenderer(Game game)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        this.game = game ?? throw new ArgumentNullException(nameof(game));
        device = game.GraphicsDevice;

        loadedTextures = new Dictionary<nint, Texture2D>();

        shader = game.Content.Load<ShaderPass>("Shaders/ImGui");
        projectionParam = shader.Parameters["Projection"]
            ?? throw new InvalidOperationException("ImGui shader has no 'Projection' constant buffer member — check Shaders/ImGui.hlsl and that the build isn't stale.");
        textureParam = shader.Parameters["DiffuseTexture"]
            ?? throw new InvalidOperationException("ImGui shader has no 'DiffuseTexture' parameter.");
        samplerParam = shader.Parameters["DiffuseSampler"]
            ?? throw new InvalidOperationException("ImGui shader has no 'DiffuseSampler' parameter.");

        sampler = device.CreateSampler(new SamplerDescription
        {
            FilterMode = SamplerFilterMode.Bilinear,
            WrapMode = SamplerWrapMode.Clamp,
        });

        pipelineState = device.CreateGraphicsState(new GraphicsStateDescription
        {
            VertexShader = shader.GetStage(ShaderStage.Vertex),
            PixelShader = shader.GetStage(ShaderStage.Pixel),
            Topology = GraphicsTopology.TriangleList,
            BlendMode = GraphicsBlendMode.Alpha,
            DepthMode = GraphicsDepthMode.Disabled,
            CullMode = GraphicsCullMode.None,
            VertexLayout = VertexLayoutCache.Get<ImGuiVertex>()
        });

        vertexCapacity = 4096;
        indexCapacity = 4096;
        vertexBuffer = new VertexBuffer<ImGuiVertex>(device, vertexCapacity);
        indexBuffer = new IndexBuffer(device, indexCapacity);

        SetupInput();
    }

    #region ImGuiRenderer

    public virtual unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        int byteCount = width * height * bytesPerPixel;
        byte[] pixels = new byte[byteCount];
        Marshal.Copy((nint)pixelData, pixels, 0, byteCount);

        Texture2D tex2d = new Texture2D(device, width, height);
        tex2d.SetData(pixels);

        if (fontTextureId.HasValue) UnbindTexture(fontTextureId.Value);

        fontTextureId = BindTexture(tex2d);

        io.Fonts.SetTexID(fontTextureId.Value);
        io.Fonts.ClearTexData();
    }

    public virtual nint BindTexture(Texture2D texture)
    {
        var id = new nint(textureId++);
        loadedTextures.Add(id, texture);
        return id;
    }

    public virtual void UnbindTexture(nint textureId)
    {
        loadedTextures.Remove(textureId);
    }

    public virtual void BeginLayout(double deltaSeconds)
    {
        ImGui.GetIO().DeltaTime = (float)Math.Max(deltaSeconds, 1e-6);

        UpdateInput();

        ImGui.NewFrame();
    }

    public virtual void EndLayout()
    {
        ImGui.Render();

        unsafe { RenderDrawData(ImGui.GetDrawData()); }
    }

    #endregion ImGuiRenderer

    #region Setup & Update

    protected virtual void SetupInput()
    {
        Game.Instance.Window.TextInput += OnTextInput;
    }

    void OnTextInput(string text)
    {
        var io = ImGui.GetIO();

        foreach (char c in text)
        {
            if (c == '\t') continue;
            io.AddInputCharacter(c);
        }
    }

    protected virtual Vector2 GetDisplaySize()
    {
        (int w, int h) = game.Window.Resolution;
        return new Vector2(w, h);
    }

    /// <summary>
    /// Sends Chisel input state to ImGui.
    /// </summary>
    protected virtual void UpdateInput()
    {
        var io = ImGui.GetIO();

        Vector2 displaySize = GetDisplaySize();
        io.DisplaySize = new System.Numerics.Vector2(displaySize.X, displaySize.Y);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

        if (!game.Window.IsFocused) return;

        Vector2 mousePos = InputManager.MousePosition;
        io.AddMousePosEvent(mousePos.X, mousePos.Y);

        io.AddMouseButtonEvent(0, InputManager.IsInputHeld(Input.MouseLeft));
        io.AddMouseButtonEvent(1, InputManager.IsInputHeld(Input.MouseRight));
        io.AddMouseButtonEvent(2, InputManager.IsInputHeld(Input.MouseMiddle));
        io.AddMouseButtonEvent(3, InputManager.IsInputHeld(Input.MouseMisc1));
        io.AddMouseButtonEvent(4, InputManager.IsInputHeld(Input.MouseMisc2));

        // Unlike XNA's cumulative, /120-scaled MouseWheelValue, SDL's wheel event is already
        // a per-frame delta in "notches" (InputManager.MouseWheelOffset resets to zero every
        // frame in InputManager.Reset()) - so this is fed straight through, no WHEEL_DELTA math.
        Vector2 wheel = InputManager.MouseWheelOffset;
        io.AddMouseWheelEvent(wheel.X, wheel.Y);

        foreach (Input key in allInputs)
        {
            if (TryMapKey(key, out ImGuiKey imguiKey))
            {
                io.AddKeyEvent(imguiKey, InputManager.IsInputHeld(key));
            }
        }
    }

    static bool TryMapKey(Input key, out ImGuiKey imguiKey)
    {
        imguiKey = key switch
        {
            Input.KeyBackspace => ImGuiKey.Backspace,
            Input.KeyTab => ImGuiKey.Tab,
            Input.KeyReturn => ImGuiKey.Enter,
            Input.KeyCapsLock => ImGuiKey.CapsLock,
            Input.KeyEscape => ImGuiKey.Escape,
            Input.KeySpace => ImGuiKey.Space,
            Input.KeyPageUp => ImGuiKey.PageUp,
            Input.KeyPageDown => ImGuiKey.PageDown,
            Input.KeyEnd => ImGuiKey.End,
            Input.KeyHome => ImGuiKey.Home,
            Input.KeyLeftArrow => ImGuiKey.LeftArrow,
            Input.KeyRightArrow => ImGuiKey.RightArrow,
            Input.KeyUpArrow => ImGuiKey.UpArrow,
            Input.KeyDownArrow => ImGuiKey.DownArrow,
            Input.KeyPrintScreen => ImGuiKey.PrintScreen,
            Input.KeyInsert => ImGuiKey.Insert,
            Input.KeyDelete => ImGuiKey.Delete,
            Input.KeyPause => ImGuiKey.Pause,

            Input.KeyNumber0 => ImGuiKey._0,
            Input.KeyNumber1 => ImGuiKey._1,
            Input.KeyNumber2 => ImGuiKey._2,
            Input.KeyNumber3 => ImGuiKey._3,
            Input.KeyNumber4 => ImGuiKey._4,
            Input.KeyNumber5 => ImGuiKey._5,
            Input.KeyNumber6 => ImGuiKey._6,
            Input.KeyNumber7 => ImGuiKey._7,
            Input.KeyNumber8 => ImGuiKey._8,
            Input.KeyNumber9 => ImGuiKey._9,

            >= Input.KeyA and <= Input.KeyZ => ImGuiKey.A + (key - Input.KeyA),
            >= Input.KeyFunction1 and <= Input.KeyFunction12 => ImGuiKey.F1 + (key - Input.KeyFunction1),

            Input.KeyKeypadMultiply => ImGuiKey.KeypadMultiply,
            Input.KeyKeypadPlus => ImGuiKey.KeypadAdd,
            Input.KeyKeypadMinus => ImGuiKey.KeypadSubtract,
            Input.KeyKeypadPeriod => ImGuiKey.KeypadDecimal,
            Input.KeyKeypadDivide => ImGuiKey.KeypadDivide,
            Input.KeyKeypadEnter => ImGuiKey.KeypadEnter,
            Input.KeyKeypadEquals => ImGuiKey.KeypadEqual,

            Input.KeyNumLock => ImGuiKey.NumLock,
            Input.KeyScrollLock => ImGuiKey.ScrollLock,
            Input.KeyLShift => ImGuiKey.LeftShift,
            Input.KeyLCtrl => ImGuiKey.LeftCtrl,
            Input.KeyLAlt => ImGuiKey.LeftAlt,
            Input.KeyLGui => ImGuiKey.LeftSuper,
            Input.KeyRShift => ImGuiKey.RightShift,
            Input.KeyRCtrl => ImGuiKey.RightCtrl,
            Input.KeyRAlt => ImGuiKey.RightAlt,
            Input.KeyRGui => ImGuiKey.RightSuper,

            Input.KeySemicolon => ImGuiKey.Semicolon,
            Input.KeyEquals => ImGuiKey.Equal,
            Input.KeyComma => ImGuiKey.Comma,
            Input.KeyMinus => ImGuiKey.Minus,
            Input.KeyPeriod => ImGuiKey.Period,
            Input.KeySlash => ImGuiKey.Slash,
            Input.KeyGrave => ImGuiKey.GraveAccent,
            Input.KeyLeftBracket => ImGuiKey.LeftBracket,
            Input.KeyRightBracket => ImGuiKey.RightBracket,
            Input.KeyBackslash => ImGuiKey.Backslash,
            Input.KeyApostrophe => ImGuiKey.Apostrophe,

            _ => ImGuiKey.None,
        };

        return imguiKey != ImGuiKey.None;
    }

    #endregion Setup & Update

    #region Internals

    unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (!drawData.Valid || drawData.CmdListsCount == 0)
        {
            return;
        }

        var io = ImGui.GetIO();

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, 0f, 1f);

        device.BindGraphicsState(pipelineState);
        projectionParam.SetValue(projection);
        shader.Apply();

        device.SetViewport(Vector2.Zero, new Vector2(io.DisplaySize.X, io.DisplaySize.Y));

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            UploadVertexBuffer(cmdList);

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                if (!loadedTextures.TryGetValue(drawCmd.TextureId, out Texture2D texture))
                {
                    throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                }

                UploadIndexSlice(cmdList, (int)drawCmd.IdxOffset, (int)drawCmd.ElemCount);

                float clipMinX = Math.Max(drawCmd.ClipRect.X, 0f);
                float clipMinY = Math.Max(drawCmd.ClipRect.Y, 0f);
                float clipMaxX = Math.Max(drawCmd.ClipRect.Z, clipMinX);
                float clipMaxY = Math.Max(drawCmd.ClipRect.W, clipMinY);

                float scissorX = clipMinX;
                float scissorY = clipMinY;

                if(device.Backend == GraphicsBackend.OpenGL)
                {
                    // GL's scissor origin is bottom-left; ClipRect is top-left-origin, Y-down.
                    scissorY = game.Window.Resolution.H - clipMaxY;
                }

                device.SetScissor(new Vector2(scissorX, scissorY), new Vector2(clipMaxX - clipMinX, clipMaxY - clipMinY));
                device.SetScissorEnabled(true);

                textureParam.SetValue(texture.Image);
                samplerParam.SetValue(sampler);

                vertexBuffer.Bind(0);
                device.SetVertexLayout(vertexBuffer.Layout, 0);
                indexBuffer.Bind();

                device.DrawIndexed(drawCmd.ElemCount);
            }
        }

        device.SetScissorEnabled(false);
    }

    unsafe void UploadVertexBuffer(ImDrawListPtr cmdList)
    {
        int vtxCount = cmdList.VtxBuffer.Size;

        if (vtxCount > vertexCapacity)
        {
            GrowVertexBuffer(vtxCount);
        }

        if (vertexScratch.Length < vtxCount)
        {
            vertexScratch = new ImGuiVertex[vtxCount];
        }

        ImDrawVert* src = (ImDrawVert*)cmdList.VtxBuffer.Data;
        for (int i = 0; i < vtxCount; i++)
        {
            ImDrawVert v = src[i];
            vertexScratch[i] = new ImGuiVertex
            {
                Position = new Vector3(v.pos.X, v.pos.Y, 0f),
                UV = new Vector2(v.uv.X, v.uv.Y),
                Color = UnpackColor(v.col),
            };
        }

        vertexBuffer.SetData(new ReadOnlySpan<ImGuiVertex>(vertexScratch, 0, vtxCount));
    }

    static Vector4 UnpackColor(uint packed)
    {
        float r = (packed & 0xFF) / 255f;
        float g = ((packed >> 8) & 0xFF) / 255f;
        float b = ((packed >> 16) & 0xFF) / 255f;
        float a = ((packed >> 24) & 0xFF) / 255f;
        return new Vector4(r, g, b, a);
    }

    unsafe void UploadIndexSlice(ImDrawListPtr cmdList, int idxOffset, int elemCount)
    {
        if (elemCount > indexCapacity)
        {
            GrowIndexBuffer(elemCount);
        }

        if (indexScratch.Length < elemCount)
        {
            indexScratch = new uint[elemCount];
        }

        ushort* src = (ushort*)cmdList.IdxBuffer.Data + idxOffset;

        for (int i = 0; i < elemCount; i++)
        {
            indexScratch[i] = src[i];
        }

        indexBuffer.SetData(new ReadOnlySpan<uint>(indexScratch, 0, elemCount));
    }

    void GrowVertexBuffer(int minimumCapacity)
    {
        while (vertexCapacity < minimumCapacity) vertexCapacity *= 2;

        vertexBuffer.Dispose();
        vertexBuffer = new VertexBuffer<ImGuiVertex>(device, vertexCapacity);
    }

    void GrowIndexBuffer(int minimumCapacity)
    {
        while (indexCapacity < minimumCapacity) indexCapacity *= 2;

        indexBuffer.Dispose();
        indexBuffer = new IndexBuffer(device, indexCapacity);
    }

    #endregion Internals
}