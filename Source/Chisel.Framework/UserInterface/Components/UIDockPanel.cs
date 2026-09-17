using System.Collections.Generic;

namespace Chisel.Framework.UI;

public enum DockSide { Top, Bottom, Left, Right, Fill }

public class UIDockPanel : UIPanel
{
    private class DockEntry
    {
        public UIObject Child;
        public DockSide Side;
        public float Size;
    }

    private List<DockEntry> entries = [];

    public UIDockPanel(UILayoutOptions options) : base(options)
    {
    }

    public void AddDocked(UIObject child, DockSide side, float size)
    {
        entries.Add(new DockEntry { Child = child, Side = side, Size = size });
        AddChild(child);
        Layout();
    }

    public void AddFill(UIObject child)
    {
        entries.Add(new DockEntry { Child = child, Side = DockSide.Fill, Size = 0 });
        AddChild(child);
        Layout();
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        Layout();
    }

    private void Layout()
    {
        var full = HalfSize;

        float top = -full.Y + Padding.Top;
        float bottom = full.Y - Padding.Bottom;
        float left = -full.X + Padding.Left;
        float right = full.X - Padding.Right;

        List<DockEntry> fillEntries = [];

        foreach (DockEntry entry in entries)
        {
            UIEdgeInsets margin = entry.Child.Margin;

            switch (entry.Side)
            {
                case DockSide.Top:
                    float topY = top + margin.Top;
                    entry.Child.HalfSizeOffset = new Vector2((right - left - margin.Left - margin.Right) / 2f, entry.Size / 2f);
                    entry.Child.CenterOffset = new Vector2((left + margin.Left + right - margin.Right) / 2f, topY + entry.Size / 2f);
                    top += entry.Size + margin.Top + margin.Bottom;
                    break;

                case DockSide.Bottom:
                    float bottomY = bottom - margin.Bottom;
                    entry.Child.HalfSizeOffset = new Vector2((right - left - margin.Left - margin.Right) / 2f, entry.Size / 2f);
                    entry.Child.CenterOffset = new Vector2((left + margin.Left + right - margin.Right) / 2f, bottomY - entry.Size / 2f);
                    bottom -= entry.Size + margin.Top + margin.Bottom;
                    break;

                case DockSide.Left:
                    float leftX = left + margin.Left;
                    entry.Child.HalfSizeOffset = new Vector2(entry.Size / 2f, (bottom - top - margin.Top - margin.Bottom) / 2f);
                    entry.Child.CenterOffset = new Vector2(leftX + entry.Size / 2f, (top + margin.Top + bottom - margin.Bottom) / 2f);
                    left += entry.Size + margin.Left + margin.Right;
                    break;

                case DockSide.Right:
                    float rightX = right - margin.Right;
                    entry.Child.HalfSizeOffset = new Vector2(entry.Size / 2f, (bottom - top - margin.Top - margin.Bottom) / 2f);
                    entry.Child.CenterOffset = new Vector2(rightX - entry.Size / 2f, (top + margin.Top + bottom - margin.Bottom) / 2f);
                    right -= entry.Size + margin.Left + margin.Right;
                    break;

                case DockSide.Fill:
                    fillEntries.Add(entry);
                    break;
            }
        }

        foreach (DockEntry entry in fillEntries)
        {
            UIEdgeInsets margin = entry.Child.Margin;

            entry.Child.HalfSizeOffset = new Vector2((right - left - margin.Left - margin.Right) / 2f, (bottom - top - margin.Top - margin.Bottom) / 2f);
            entry.Child.CenterOffset = new Vector2((left + margin.Left + right - margin.Right) / 2f, (top + margin.Top + bottom - margin.Bottom) / 2f);
        }
    }
}