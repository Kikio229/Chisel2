using System;

namespace Chisel.Framework.UI;

public class UIFourWaySplitView : UIPanel
{
    public UIPanel TopLeft { get; }
    public UIPanel TopRight { get; }
    public UIPanel BottomLeft { get; }
    public UIPanel BottomRight { get; }

    public float SplitX = 0.5f; // 0..1
    public float SplitY = 0.5f; // 0..1
    public float SplitterThickness = 6f;
    public float MinPaneSize = 32f;

    private bool draggingX;
    private bool draggingY;

    public UIFourWaySplitView(UILayoutOptions options) : base(options)
    {
        TopLeft = new UIPanel(options) { Anchor = UIAnchor.Center };
        TopRight = new UIPanel(options) { Anchor = UIAnchor.Center };
        BottomLeft = new UIPanel(options) { Anchor = UIAnchor.Center };
        BottomRight = new UIPanel(options) { Anchor = UIAnchor.Center };

        AddChild(TopLeft);
        AddChild(TopRight);
        AddChild(BottomLeft);
        AddChild(BottomRight);
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        var full = HalfSize;
        var pos = Position;
        float half = SplitterThickness / 2f;

        float splitPxX = -full.X + full.X * 2f * SplitX;
        float splitPxY = -full.Y + full.Y * 2f * SplitY;

        var vBar = new Rectangle((int)(pos.X + splitPxX - half), (int)(pos.Y - full.Y), (int)SplitterThickness, (int)(full.Y * 2f));
        var hBar = new Rectangle((int)(pos.X - full.X), (int)(pos.Y + splitPxY - half), (int)(full.X * 2f), (int)SplitterThickness);
        var center = new Rectangle((int)(pos.X + splitPxX - half * 3), (int)(pos.Y + splitPxY - half * 3), (int)(half * 6), (int)(half * 6));

        bool mouseDown = InputManager.IsInputHeld(Input.MouseLeft);

        if (draggingX || draggingY)
        {
            if (!mouseDown)
            {
                draggingX = false;
                draggingY = false;
            }
            else
            {
                if (draggingX)
                {
                    float minR = MinPaneSize / (full.X * 2f);
                    SplitX = Math.Clamp(SplitX + InputManager.MouseDelta.X / (full.X * 2f), minR, 1f - minR);
                }
                if (draggingY)
                {
                    float minR = MinPaneSize / (full.Y * 2f);
                    SplitY = Math.Clamp(SplitY + InputManager.MouseDelta.Y / (full.Y * 2f), minR, 1f - minR);
                }
            }
        }
        else if (IsMouseOverRectPublic(center))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeAll);
            if (mouseDown) { draggingX = true; draggingY = true; }
        }
        else if (IsMouseOverRectPublic(vBar))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeWE);
            if (mouseDown) draggingX = true;
        }
        else if (IsMouseOverRectPublic(hBar))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNS);
            if (mouseDown) draggingY = true;
        }
        else if (Game.Instance.Window.GetCursorStyle() != CursorStyle.Arrow)
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.Arrow);
        }

        Layout();
    }

    private static bool IsMouseOverRectPublic(Rectangle rect) => InputManager.MousePosition.X >= rect.X
        && InputManager.MousePosition.X <= rect.X + rect.Width
        && InputManager.MousePosition.Y >= rect.Y
        && InputManager.MousePosition.Y <= rect.Y + rect.Height;

    private void Layout()
    {
        var full = HalfSize;
        float half = SplitterThickness / 2f;

        float splitPxX = -full.X + full.X * 2f * SplitX;
        float splitPxY = -full.Y + full.Y * 2f * SplitY;

        float leftW = splitPxX + full.X - half;
        float rightW = full.X * 2f - leftW - SplitterThickness;
        float topH = splitPxY + full.Y - half;
        float bottomH = full.Y * 2f - topH - SplitterThickness;

        TopLeft.HalfSizeOffset = new Vector2(leftW / 2f, topH / 2f);
        TopLeft.CenterOffset = new Vector2(-full.X + leftW / 2f, -full.Y + topH / 2f);

        TopRight.HalfSizeOffset = new Vector2(rightW / 2f, topH / 2f);
        TopRight.CenterOffset = new Vector2(full.X - rightW / 2f, -full.Y + topH / 2f);

        BottomLeft.HalfSizeOffset = new Vector2(leftW / 2f, bottomH / 2f);
        BottomLeft.CenterOffset = new Vector2(-full.X + leftW / 2f, full.Y - bottomH / 2f);

        BottomRight.HalfSizeOffset = new Vector2(rightW / 2f, bottomH / 2f);
        BottomRight.CenterOffset = new Vector2(full.X - rightW / 2f, full.Y - bottomH / 2f);
    }
}