using System.Collections.Generic;
using System.Linq;

namespace Chisel.Framework.UI;

public abstract class UIObject(UILayoutOptions options)
{
    public UIObject Parent;
    public List<UIObject> Children = [];
    public Vector2 CenterOffset;
    public Vector2 HalfSizeOffset;
    public Color Tint = Color.White;
    public UIAnchor Anchor;
    public float RotationOffset;

    public abstract bool AllowNavigatingTo { get; }
    public abstract bool AbsorbInputs { get; }

    public bool IsHighlighted { get; private set; }
    public bool IsSelected { get; private set; }

    public UILayoutOptions LayoutOptions { get; set; } = options;

    public Vector2 Right
    {
        get
        {
            return new(float.Cos(RotationOffset), float.Sin(RotationOffset));
        }
    }
    public Vector2 Up
    {
        get
        {
            // i love this formula
            return new(-float.Sin(RotationOffset), float.Cos(RotationOffset));
        }
    }
    public float Rotation
    {
        get
        {
            return Parent?.Rotation ?? 0 + RotationOffset;
        }
    }
    public Vector2 Position
    {
        get
        {
            if (Parent == null) return CenterOffset;

            var pPos = Parent.Position;
            var pUp = Parent.Up;
            var pRight = Parent.Right;
            var pDim = Parent.HalfSize;

            var localPos = CenterOffset;

            if (Anchor.HasFlag(UIAnchor.Left))
            {
                localPos.X = -(pDim.X - HalfSize.X) + CenterOffset.X;
            }
            if (Anchor.HasFlag(UIAnchor.Right))
            {
                localPos.X = (pDim.X - HalfSize.X) + CenterOffset.X;
            }
            if (Anchor.HasFlag(UIAnchor.Top))
            {
                localPos.Y = -(pDim.Y - HalfSize.Y) + CenterOffset.Y;
            }
            if (Anchor.HasFlag(UIAnchor.Bottom))
            {
                localPos.Y = (pDim.Y - HalfSize.Y) + CenterOffset.Y;
            }

            return pRight * localPos.X + pUp * localPos.Y + pPos;
        }
    }
    public Vector2 HalfSize
    {
        get
        {
            if (Parent == null) return HalfSizeOffset;

            var pPos = Parent.Position;
            var pDim = Parent.HalfSizeOffset;

            var size = HalfSizeOffset;

            if(Anchor.HasFlag(UIAnchor.FillH))
            {
                size.X = pDim.X + HalfSizeOffset.X;
            }
            if (Anchor.HasFlag(UIAnchor.FillV))
            {
                size.Y = pDim.Y + HalfSizeOffset.Y;
            }

            return size;
        }
    }


    // Gamepad navigation. These are implemented as a tree-walk so that
    // you can navigate into a window, navigate all of the elements inside,
    // and then after exhausting all options inside that window you can
    // move to the next.
    public UIObject GetPreviousSibling()
    {
        if (LayoutOptions.PreviousSibling != null) return LayoutOptions.PreviousSibling;
        if (Parent == null) return null;

        return Parent.GetPreviousSibling();
    }

    public UIObject GetNextSibling()
    {
        // Rather than immediately jump to the next high-level object,
        // jump to the inner object. Of course we only want to jump
        // to a child that allows navigation, so not something like a
        // label.
        if (Children != null && Children.Count > 0 && Children.Any(c => c.AllowNavigatingTo))
            return Children.First(c => c.AllowNavigatingTo);

        if (LayoutOptions.NextSibling != null) return LayoutOptions.NextSibling;
        if (Parent == null) return null;

        return Parent.GetNextSibling();
    }

    public void Update(float dt)
    {
        if(IsMouseOverRect(new Rectangle(Position - HalfSize, Position + HalfSize)))
        {
            if (!IsHighlighted) OnHighlighted();
            IsHighlighted = true;
        }
        else
        {
            if (IsHighlighted) OnUnhighlighted();
            IsHighlighted = false;
        }

        if(IsHighlighted && InputManager.IsInputHeld(Input.MouseLeft))
        {
            if (!IsSelected)
            {
                OnPrimaryClicked();
            }
            IsSelected = true;
        }
        else if (IsHighlighted && InputManager.IsInputHeld(Input.MouseRight))
        {
            if (!IsSelected)
            {
                OnSecondaryClicked();
            }
            IsSelected = true;
        }
        else
        {
            IsSelected = false;
        }

        OnUpdate(dt);

        for(int i = Children.Count-1; i >= 0; i--)
        {
            Children[i].Update(dt);
        }
    }
    public void Render(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        OnRender(dt, batch, atlasTexture);

        for (int i = Children.Count - 1; i >= 0; i--)
        {
            Children[i].Render(dt, batch, atlasTexture);
        }
    }

    public abstract void OnUpdate(float dt);
    public abstract void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture);
    public abstract void OnHighlighted();
    public abstract void OnUnhighlighted();
    public abstract void OnPrimaryClicked();
    public abstract void OnSecondaryClicked();

    protected static bool IsMouseOverRect(Rectangle rect)
    {
        var mouse = InputManager.MousePosition;

        return rect.ContainsVector(mouse);
    }

    public void AddChild(UIObject child)
    {
        child.Parent = this;
        Children.Add(child);
    }
    public void RemoveChild(UIObject child)
    {
        child.Parent = null;
        Children.Remove(child);
    }
}
