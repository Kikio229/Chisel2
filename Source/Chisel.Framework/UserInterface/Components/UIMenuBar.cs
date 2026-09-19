using System;
using System.Collections.Generic;

namespace Chisel.Framework.UI;

public class UIMenuItem
{
    public string Text;
    public Action OnSelected;

    public UIMenuItem(string text, Action onSelected)
    {
        Text = text;
        OnSelected = onSelected;
    }
}

public class UIMenuBarMenu
{
    public string Title;
    public List<UIMenuItem> Items;

    public UIMenuBarMenu(string title, List<UIMenuItem> items)
    {
        Title = title;
        Items = items;
    }
}

public class UIMenuBar : UIPanel
{
    public float MenuWidth = 80f;
    public float FontSize = 20f;
    public float RowHeight = 28f;

    private List<UIMenuBarMenu> menus = [];
    private UIPanel openPopup;
    private int openIndex = -1;

    public Color ChildItemTint = new Color(0.05f, 0.05f, 0.05f);
    public Color SelectTint = new Color(0.2f, 0.2f, 0.2f);
    public Color HighlightTint = new Color(0.3f, 0.3f, 0.3f);

    public UIMenuBar(UILayoutOptions options) : base(options)
    {
    }

    public void SetMenus(List<UIMenuBarMenu> newMenus)
    {
        CloseMenu();

        foreach (UIObject child in Children.ToArray())
        {
            RemoveChild(child);
        }

        menus = newMenus;

        for (int i = 0; i < menus.Count; i++)
        {
            int index = i;

            var button = new UIButton(LayoutOptions)
            {
                Anchor = UIAnchor.Left | UIAnchor.FillV,
                HalfSizeOffset = new Vector2(MenuWidth / 2f, 0),
                CenterOffset = new Vector2(i * MenuWidth, 0),
                Style = ButtonStyle.Flat,
                BaseTint = Color.Transparent,
                SelectTint = SelectTint,
                HighlightTint = HighlightTint,
            };

            button.OnClicked += () => ToggleMenu(index);

            var label = new UITextBlock(LayoutOptions) { Anchor = UIAnchor.Center, Text = menus[index].Title, FontSize = FontSize };
            button.AddChild(label);

            AddChild(button);
        }
    }

    private void ToggleMenu(int index)
    {
        if (openIndex == index)
        {
            CloseMenu();
            return;
        }

        CloseMenu();

        openIndex = index;
        var items = menus[index].Items;

        openPopup = new UIPanel(LayoutOptions) { Anchor = UIAnchor.Left };
        openPopup.HalfSizeOffset = new Vector2(MenuWidth, items.Count * RowHeight / 2f);
        openPopup.CenterOffset = new Vector2(index * MenuWidth, HalfSize.Y + openPopup.HalfSizeOffset.Y);
        openPopup.Tint = new Color(30, 30, 30);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var row = new UIButton(LayoutOptions)
            {
                HalfSizeOffset = new Vector2(openPopup.HalfSizeOffset.X, RowHeight / 2f),
                CenterOffset = new Vector2(0, -openPopup.HalfSizeOffset.Y + RowHeight / 2f + i * RowHeight),
                Style = ButtonStyle.Flat,
                BaseTint = ChildItemTint,
                SelectTint = SelectTint,
                HighlightTint = HighlightTint,
            };

            row.OnClicked += () =>
            {
                item.OnSelected?.Invoke();
                CloseMenu();
            };

            var label = new UITextBlock(LayoutOptions) { Anchor = UIAnchor.Left, Text = item.Text, FontSize = FontSize };
            row.AddChild(label);

            openPopup.AddChild(row);
        }

        AddChildOnTop(openPopup);
    }

    private void CloseMenu()
    {
        if (openPopup != null)
        {
            RemoveChild(openPopup);
            openPopup = null;
        }

        openIndex = -1;
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        if (openPopup == null || !InputManager.IsInputPressed(Input.MouseLeft))
        {
            return;
        }

        bool overBar = IsHighlighted;
        bool overPopup = false;

        foreach (UIObject child in openPopup.Children)
        {
            if (child is UIButton b && b.IsHighlighted)
            {
                overPopup = true;
            }
        }

        if (!overBar && !overPopup)
        {
            CloseMenu();
        }
    }
}