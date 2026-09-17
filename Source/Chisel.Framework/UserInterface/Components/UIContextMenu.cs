using System.Collections.Generic;

namespace Chisel.Framework.UI;

public class UIContextMenu : UIPanel
{
    public float RowHeight = 28f;
    public float FontSize = 20f;

    public UIContextMenu(UILayoutOptions options) : base(options)
    {
        Visible = false;
        Tint = new Color(30, 30, 30);
    }

    public override bool AbsorbInputs => Visible;

    public void Show(Vector2 screenPosition, List<UIMenuItem> items)
    {
        foreach (UIObject child in Children.ToArray())
        {
            RemoveChild(child);
        }

        HalfSizeOffset = new Vector2(120f, items.Count * RowHeight / 2f);

        Vector2 parentPos = Parent.Position;
        Vector2 parentRight = Parent.Right;
        Vector2 parentUp = Parent.Up;
        Vector2 delta = screenPosition - parentPos;

        float localX = delta.X * parentRight.X + delta.Y * parentRight.Y;
        float localY = delta.X * parentUp.X + delta.Y * parentUp.Y;

        CenterOffset = new Vector2(localX, localY) + HalfSizeOffset;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var row = new UIButton(LayoutOptions)
            {
                HalfSizeOffset = new Vector2(HalfSizeOffset.X, RowHeight / 2f),
                CenterOffset = new Vector2(0, -HalfSizeOffset.Y + RowHeight / 2f + i * RowHeight),
            };

            row.OnClicked += () =>
            {
                item.OnSelected?.Invoke();
                Hide();
            };

            var label = new UITextBlock(LayoutOptions) { Anchor = UIAnchor.Left, Text = item.Text, FontSize = FontSize };
            row.AddChild(label);

            AddChild(row);
        }

        Visible = true;
    }

    public void Hide()
    {
        Visible = false;

        foreach (UIObject child in Children.ToArray())
        {
            RemoveChild(child);
        }
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        if (!Visible || !InputManager.IsInputPressed(Input.MouseLeft))
        {
            return;
        }

        bool overSelf = IsHighlighted;
        bool overChild = false;

        foreach (UIObject child in Children)
        {
            if (child is UIButton b && b.IsHighlighted)
            {
                overChild = true;
            }
        }

        if (!overSelf && !overChild)
        {
            Hide();
        }
    }
}