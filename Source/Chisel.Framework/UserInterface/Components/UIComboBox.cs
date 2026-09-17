using System;
using System.Collections.Generic;

namespace Chisel.Framework.UI;

public class UIComboBox : UIPanel
{
    public List<string> Items = [];
    public int SelectedIndex = -1;
    public float FontSize = 20f;
    public float RowHeight = 28f;

    public event Action<int> OnSelectionChanged;

    private bool isOpen;
    private UIPanel popup;

    public UIComboBox(UILayoutOptions options) : base(options)
    {
    }

    public override Rectangle PanelRect => IsHighlighted
        ? new Rectangle(352, 32, 96, 32)
        : new Rectangle(352, 0, 96, 32);

    public override void OnPrimaryClicked()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        if (isOpen && InputManager.IsInputPressed(Input.MouseLeft) && !IsHighlighted && !AnyRowHighlighted())
        {
            Close();
        }
    }

    private bool AnyRowHighlighted()
    {
        if (popup == null)
        {
            return false;
        }

        foreach (UIObject child in popup.Children)
        {
            if (child is UIButton b && b.IsHighlighted)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        base.OnRender(dt, batch, atlasTexture);

        Vector2 topLeft = ContentTopLeft;
        var pos = Position;
        string label = SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : string.Empty;

        Vector2 measured = batch.MeasureText(label, (int)FontSize);
        batch.DrawString(label, (int)FontSize, new Vector2(topLeft.X, pos.Y - measured.Y / 2f), Color.White);
    }

    private void Open()
    {
        isOpen = true;

        popup = new UIPanel(LayoutOptions) { Anchor = UIAnchor.Top };
        popup.HalfSizeOffset = new Vector2(HalfSizeOffset.X, Items.Count * RowHeight / 2f);
        popup.CenterOffset = new Vector2(0, HalfSize.Y * 2f + popup.HalfSizeOffset.Y);
        popup.Tint = new Color(30, 30, 30);

        for (int i = 0; i < Items.Count; i++)
        {
            int index = i;

            var row = new UIButton(LayoutOptions)
            {
                HalfSizeOffset = new Vector2(popup.HalfSizeOffset.X, RowHeight / 2f),
                CenterOffset = new Vector2(0, -popup.HalfSizeOffset.Y + RowHeight / 2f + i * RowHeight),
            };

            row.OnClicked += () =>
            {
                SelectedIndex = index;
                OnSelectionChanged?.Invoke(index);
                Close();
            };

            var label = new UITextBlock(LayoutOptions) { Anchor = UIAnchor.Left, Text = Items[i], FontSize = FontSize };
            row.AddChild(label);

            popup.AddChild(row);
        }

        AddChildOnTop(popup);
    }

    private void Close()
    {
        if (popup != null)
        {
            RemoveChild(popup);
            popup = null;
        }

        isOpen = false;
    }
}