using Chisel.Framework.UserInterface.Components;
using System;

namespace Chisel.Framework.UI;
public class UIWindow : UIPanel
{
    public string Title { get; private set; }
    public UIPanel InnerPanel { get; private set; }
    public Vector2 MinSize { get; private set; } = new(128);
    public UIWindow(string title, UILayoutOptions options) : base(options)
    {
        this.Title = title;
        InnerPanel = new UIPanel(options);
        InnerPanel.Tint = new(10,10,10);

        ResizeContent();

        Children.Add(InnerPanel);
        InnerPanel.Parent = this;

        Tint = new(140,140,140);

        var btn = new UIButton(options)
        {
            HalfSizeOffset = new(16, 8),
            CenterOffset = new(-12, 7),
            Anchor = UIAnchor.Top | UIAnchor.Right,
            Tint = Color.LightSalmon
        };

        Children.Add(btn);
        btn.Parent = this;
    }

    public override Rectangle PanelRect => new(0,0,96,96);

    [Flags]
    protected enum SizeMode
    {
        Top = 1<<0,
        Bottom = 1<<1,
        Left = 1<<2,
        Right = 1<<3,
        Move = 1<<4,
    }

    protected bool IsSizing;
    protected SizeMode Mode;

    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        base.OnRender(dt, batch, atlasTexture);

        batch.DrawString(Title, 18, Position - HalfSize + new Vector2(8,3), Color.Black, FontStashSharp.FontSystemEffect.Blurry, 3);
        batch.DrawString(Title, 18, Position - HalfSize + new Vector2(10,5), Color.White);
    }
    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        ResizeContent();
        
        var topCenter = Position - new Vector2(0, HalfSize.Y);
        var bottomCenter = Position + new Vector2(0, HalfSize.Y);
        var leftCenter = Position - new Vector2(HalfSize.X, 0);
        var rightCenter = Position + new Vector2(HalfSize.X, 0);

        var tr = new Rectangle((int)rightCenter.X - 12, (int)topCenter.Y - 4, 16, 16);
        var tl = new Rectangle((int)leftCenter.X - 4, (int)topCenter.Y - 4, 16, 16);
        var br = new Rectangle((int)rightCenter.X - 12, (int)bottomCenter.Y - 12, 16, 16);
        var bl = new Rectangle((int)leftCenter.X - 4, (int)bottomCenter.Y - 12, 16, 16);

        var l = new Rectangle((int)leftCenter.X, (int)leftCenter.Y - (int)HalfSize.Y, 16, (int)HalfSize.Y * 2);
        var r = new Rectangle((int)rightCenter.X, (int)rightCenter.Y - (int)HalfSize.Y, 16, (int)HalfSize.Y * 2);
        var t = new Rectangle((int)bottomCenter.X - (int)HalfSize.X, (int)topCenter.Y - 4, (int)HalfSize.X * 2, 16);
        var b = new Rectangle((int)bottomCenter.X - (int)HalfSize.X, (int)bottomCenter.Y - 12, (int)HalfSize.X * 2, 16);

        var titlebar = new Rectangle((int)bottomCenter.X - (int)HalfSize.X, (int)topCenter.Y, (int)HalfSize.X * 2, 30);

        bool mouseDown = InputManager.IsInputHeld(Input.MouseLeft);

        if(IsSizing)
        {
            if (!mouseDown) IsSizing = false;

            Vector2 sizeChange = Vector2.Zero;
            Vector2 posChange = Vector2.Zero;

            if (Mode == SizeMode.Move)
            {
                CenterOffset += InputManager.MouseDelta;
            }
            else
            {
                if (Mode.HasFlag(SizeMode.Right))
                {
                    sizeChange.X += InputManager.MouseDelta.X / 2;
                    posChange.X = 1;
                }
                if (Mode.HasFlag(SizeMode.Left))
                {
                    sizeChange.X -= InputManager.MouseDelta.X / 2;
                    posChange.X = -1;
                }
                if (Mode.HasFlag(SizeMode.Bottom))
                {
                    sizeChange.Y += InputManager.MouseDelta.Y / 2;
                    posChange.Y = 1;
                }
                if (Mode.HasFlag(SizeMode.Top))
                {
                    sizeChange.Y -= InputManager.MouseDelta.Y / 2;
                    posChange.Y = -1;
                }
            }

            sizeChange.X = float.Max(sizeChange.X, MinSize.X - HalfSize.X);
            sizeChange.Y = float.Max(sizeChange.Y, MinSize.Y - HalfSize.Y);

            HalfSizeOffset += sizeChange;
            CenterOffset += posChange * sizeChange;

            ResizeContent();

            return;
        }

        if (IsMouseOverRect(tr))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNE);

            if(mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Top | SizeMode.Right;
            }
        }
        else if(IsMouseOverRect(tl))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNW);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Top | SizeMode.Left;
            }
        }
        else if (IsMouseOverRect(br))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNW);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Bottom | SizeMode.Right;
            }
        }
        else if (IsMouseOverRect(bl))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNE);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Bottom | SizeMode.Left;
            }
        }
        else if (IsMouseOverRect(b))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNS);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Bottom;
            }
        }
        else if (IsMouseOverRect(t))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeNS);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Top;
            }
        }
        else if (IsMouseOverRect(l))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeWE);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Left;
            }
        }
        else if (IsMouseOverRect(r))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.SizeWE);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Right;
            }
        }
        else if (IsMouseOverRect(titlebar))
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.Arrow);

            if (mouseDown)
            {
                IsSizing = true;
                Mode = SizeMode.Move;
            }
        }
        else if(Game.Instance.Window.GetCursorStyle() != CursorStyle.Arrow)
        {
            Game.Instance.Window.SetCursorStyle(CursorStyle.Arrow);
        }
    }
    void ResizeContent()
    {
        var windowW = HalfSizeOffset.X - 13;
        var windowH = HalfSizeOffset.Y - 22;

        InnerPanel.HalfSizeOffset = new(windowW, windowH);
        InnerPanel.CenterOffset = new Vector2(0,8);
    }
}
