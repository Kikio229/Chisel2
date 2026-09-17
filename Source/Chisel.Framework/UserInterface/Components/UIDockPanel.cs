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

        float top = -full.Y;
        float bottom = full.Y;
        float left = -full.X;
        float right = full.X;

        List<DockEntry> fillEntries = [];

        foreach (DockEntry entry in entries)
        {
            switch (entry.Side)
            {
                case DockSide.Top:
                    entry.Child.HalfSizeOffset = new Vector2((right - left) / 2f, entry.Size / 2f);
                    entry.Child.CenterOffset = new Vector2((left + right) / 2f, top + entry.Size / 2f);
                    top += entry.Size;
                    break;

                case DockSide.Bottom:
                    entry.Child.HalfSizeOffset = new Vector2((right - left) / 2f, entry.Size / 2f);
                    entry.Child.CenterOffset = new Vector2((left + right) / 2f, bottom - entry.Size / 2f);
                    bottom -= entry.Size;
                    break;

                case DockSide.Left:
                    entry.Child.HalfSizeOffset = new Vector2(entry.Size / 2f, (bottom - top) / 2f);
                    entry.Child.CenterOffset = new Vector2(left + entry.Size / 2f, (top + bottom) / 2f);
                    left += entry.Size;
                    break;

                case DockSide.Right:
                    entry.Child.HalfSizeOffset = new Vector2(entry.Size / 2f, (bottom - top) / 2f);
                    entry.Child.CenterOffset = new Vector2(right - entry.Size / 2f, (top + bottom) / 2f);
                    right -= entry.Size;
                    break;

                case DockSide.Fill:
                    fillEntries.Add(entry);
                    break;
            }
        }

        foreach (DockEntry entry in fillEntries)
        {
            entry.Child.HalfSizeOffset = new Vector2((right - left) / 2f, (bottom - top) / 2f);
            entry.Child.CenterOffset = new Vector2((left + right) / 2f, (top + bottom) / 2f);
        }
    }
}