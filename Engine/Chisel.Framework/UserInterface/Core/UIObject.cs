using System.Linq;

namespace Chisel.Framework.UI;

public abstract class UIObject(UILayoutOptions options)
{
    public UIObject Parent;
    public UIObject[] Children;
    public Vector2 CenterOffset;
    public Vector2 HalfExtents;
    public UIAnchor Anchor;
    public float RotationOffset;

    public abstract bool AllowNavigatingTo { get; }
    public abstract bool AbsorbInputs { get; }

    public UILayoutOptions LayoutOptions { get; set; } = options;

    protected bool IsHighlighted { get; set; }

    public Vector2 Up
    {
        get
        {
            return new(float.Cos(RotationOffset), float.Sin(RotationOffset));
        }
    }
    public Vector2 Right
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
            var pDim = Parent.HalfExtents;

            var localPos = CenterOffset;

            if ((Anchor & UIAnchor.Left) == 0)
            {
                localPos.X = -(pDim.X - HalfExtents.X) + CenterOffset.X;
            }
            if ((Anchor & UIAnchor.Right) == 0)
            {
                localPos.X = (pDim.X - HalfExtents.X) + CenterOffset.X;
            }
            if ((Anchor & UIAnchor.Bottom) == 0)
            {
                localPos.Y = -(pDim.Y - HalfExtents.Y) + CenterOffset.Y;
            }
            if ((Anchor & UIAnchor.Top) == 0)
            {
                localPos.Y = (pDim.Y - HalfExtents.Y) + CenterOffset.Y;
            }

            return pRight * localPos.X + pUp * localPos.Y;
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
        if (Children != null && Children.Length > 0 && Children.Any(c => c.AllowNavigatingTo))
            return Children.First(c => c.AllowNavigatingTo);

        if (LayoutOptions.NextSibling != null) return LayoutOptions.NextSibling;
        if (Parent == null) return null;

        return Parent.GetNextSibling();
    }

    public abstract void OnUpdate(float dt);
    public abstract void OnRender(float dt);
    public abstract void OnHighlighted();
    public abstract void OnUnhighlighted();
    public abstract void OnPrimaryClicked();
    public abstract void OnSecondaryClicked();
}
