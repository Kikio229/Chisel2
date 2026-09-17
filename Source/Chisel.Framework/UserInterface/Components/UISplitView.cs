using System;

namespace Chisel.Framework.UI;

public enum SplitOrientation { Horizontal, Vertical }

public class UISplitView : UIPanel
{
    public UIPanel First { get; }
    public UIPanel Second { get; }

    public SplitOrientation Orientation;
    public float SplitRatio = 0.5f;
    public float SplitterThickness = 6f;
    public float MinPaneSize = 32f;

    private bool dragging;

    public UISplitView(UILayoutOptions options, SplitOrientation orientation) : base(options)
    {
        Orientation = orientation;

        First = new UIPanel(options) { Anchor = UIAnchor.Center };
        Second = new UIPanel(options) { Anchor = UIAnchor.Center };

        AddChild(First);
        AddChild(Second);
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        var full = HalfSize;
        var pos = Position;
        float half = SplitterThickness / 2f;
        bool mouseDown = InputManager.IsInputHeld(Input.MouseLeft);

        if (Orientation == SplitOrientation.Vertical)
        {
            float splitPxX = -full.X + full.X * 2f * SplitRatio;
            var bar = new Rectangle((int)(pos.X + splitPxX - half), (int)(pos.Y - full.Y), (int)SplitterThickness, (int)(full.Y * 2f));

            if (dragging)
            {
                if (!mouseDown)
                {
                    dragging = false;
                }
                else
                {
                    float minR = MinPaneSize / (full.X * 2f);
                    SplitRatio = Math.Clamp(SplitRatio + InputManager.MouseDelta.X / (full.X * 2f), minR, 1f - minR);
                }
            }
            else if (IsMouseOverRectPublic(bar))
            {
                Game.Instance.Window.SetCursorStyle(CursorStyle.SizeWE);
                if (mouseDown)
                {
                    dragging = true;
                }
            }
            else if (Game.Instance.Window.GetCursorStyle() != CursorStyle.Arrow)
            {
                Game.Instance.Window.SetCursorStyle(CursorStyle.Arrow);
            }
        }
        else
        {
            float splitPxY = -full.Y + full.Y * 2f * SplitRatio;
            var bar = new Rectangle((int)(pos.X - full.X), (int)(pos.Y + splitPxY - half), (int)(full.X * 2f), (int)SplitterThickness);

            if (dragging)
            {
                if (!mouseDown)
                {
                    dragging = false;
                }
                else
                {
                    float minR = MinPaneSize / (full.Y * 2f);
                    SplitRatio = Math.Clamp(SplitRatio + InputManager.MouseDelta.Y / (full.Y * 2f), minR, 1f - minR);
                }
            }
            else if (IsMouseOverRectPublic(bar))
            {
                Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNS);
                if (mouseDown)
                {
                    dragging = true;
                }
            }
            else if (Game.Instance.Window.GetCursorStyle() != CursorStyle.Arrow)
            {
                Game.Instance.Window.SetCursorStyle(CursorStyle.Arrow);
            }
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

        if (Orientation == SplitOrientation.Vertical)
        {
            float splitPxX = -full.X + full.X * 2f * SplitRatio;

            float firstW = splitPxX + full.X - half;
            float secondW = full.X * 2f - firstW - SplitterThickness;

            First.HalfSizeOffset = new Vector2(firstW / 2f, full.Y);
            First.CenterOffset = new Vector2(-full.X + firstW / 2f, 0);

            Second.HalfSizeOffset = new Vector2(secondW / 2f, full.Y);
            Second.CenterOffset = new Vector2(full.X - secondW / 2f, 0);
        }
        else
        {
            float splitPxY = -full.Y + full.Y * 2f * SplitRatio;

            float firstH = splitPxY + full.Y - half;
            float secondH = full.Y * 2f - firstH - SplitterThickness;

            First.HalfSizeOffset = new Vector2(full.X, firstH / 2f);
            First.CenterOffset = new Vector2(0, -full.Y + firstH / 2f);

            Second.HalfSizeOffset = new Vector2(full.X, secondH / 2f);
            Second.CenterOffset = new Vector2(0, full.Y - secondH / 2f);
        }
    }
}