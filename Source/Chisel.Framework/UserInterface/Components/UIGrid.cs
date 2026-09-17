using System;
using System.Collections.Generic;

namespace Chisel.Framework.UI;

public struct GridTrack
{
    public float Size;
    public bool IsStar;

    public static GridTrack Px(float px) => new() { Size = px, IsStar = false };
    public static GridTrack Star(float weight = 1f) => new() { Size = weight, IsStar = true };
}

public class UIGrid : UIPanel
{
    private readonly List<GridTrack> columns = new();
    private readonly List<GridTrack> rows = new();
    private readonly List<(UIObject obj, int row, int col, int rowSpan, int colSpan)> cells = new();

    public float[] ColumnSizes { get; private set; } = Array.Empty<float>();
    public float[] RowSizes { get; private set; } = Array.Empty<float>();
    private float[] colOffsets = Array.Empty<float>();
    private float[] rowOffsets = Array.Empty<float>();

    public UIGrid(UILayoutOptions options) : base(options) { }

    public void AddColumn(GridTrack track) => columns.Add(track);
    public void AddRow(GridTrack track) => rows.Add(track);

    public void Place(UIObject obj, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        obj.Anchor = UIAnchor.Center;
        cells.Add((obj, row, col, rowSpan, colSpan));
        AddChild(obj);
    }

    // Splitter sits on the boundary before columns[index] / rows[index] and
    // resizes that track against the one before it.
    public void AddColumnSplitter(int beforeColumnIndex) => AddChild(new UIGridSplitter(default, this, beforeColumnIndex, vertical: true));
    public void AddRowSplitter(int beforeRowIndex) => AddChild(new UIGridSplitter(default, this, beforeRowIndex, vertical: false));

    public void SetColumnSize(int index, float px) { var t = columns[index]; t.Size = Math.Max(0, px); t.IsStar = false; columns[index] = t; }
    public void SetRowSize(int index, float px) { var t = rows[index]; t.Size = Math.Max(0, px); t.IsStar = false; rows[index] = t; }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        Layout();
    }

    private void Layout()
    {
        var full = HalfSize;

        ColumnSizes = ResolveTracks(columns, full.X * 2f);
        RowSizes = ResolveTracks(rows, full.Y * 2f);
        colOffsets = Accumulate(ColumnSizes);
        rowOffsets = Accumulate(RowSizes);

        foreach (var (obj, row, col, rowSpan, colSpan) in cells)
        {
            int colEnd = Math.Min(col + colSpan, colOffsets.Length - 1);
            int rowEnd = Math.Min(row + rowSpan, rowOffsets.Length - 1);

            float x0 = colOffsets[col], x1 = colOffsets[colEnd];
            float y0 = rowOffsets[row], y1 = rowOffsets[rowEnd];

            obj.HalfSizeOffset = new Vector2((x1 - x0) / 2f, (y1 - y0) / 2f);
            obj.CenterOffset = new Vector2(x0 + (x1 - x0) / 2f - full.X, y0 + (y1 - y0) / 2f - full.Y);
        }
    }

    // Local offset (relative to this grid's center) of the boundary before
    // the given track index.
    public float GetColumnBoundaryOffset(int index) => colOffsets.Length > index ? colOffsets[index] - HalfSize.X : 0;
    public float GetRowBoundaryOffset(int index) => rowOffsets.Length > index ? rowOffsets[index] - HalfSize.Y : 0;

    private static float[] ResolveTracks(List<GridTrack> tracks, float total)
    {
        var sizes = new float[tracks.Count];
        float fixedTotal = 0, starWeight = 0;

        foreach (var t in tracks)
        {
            if (t.IsStar) starWeight += t.Size;
            else fixedTotal += t.Size;
        }

        float remaining = Math.Max(0, total - fixedTotal);

        for (int i = 0; i < tracks.Count; i++)
            sizes[i] = tracks[i].IsStar ? (starWeight > 0 ? remaining * (tracks[i].Size / starWeight) : 0) : tracks[i].Size;

        return sizes;
    }

    private static float[] Accumulate(float[] sizes)
    {
        var offsets = new float[sizes.Length + 1];
        for (int i = 0; i < sizes.Length; i++) offsets[i + 1] = offsets[i] + sizes[i];
        return offsets;
    }
}

public class UIGridSplitter : UIPanel
{
    private readonly UIGrid grid;
    private readonly int trackIndex; // track after the boundary; trackIndex-1 is the one before it
    private readonly bool vertical;

    public float Thickness = 6f;

    public UIGridSplitter(UILayoutOptions options, UIGrid grid, int trackIndex, bool vertical) : base(options)
    {
        this.grid = grid;
        this.trackIndex = trackIndex;
        this.vertical = vertical;
        Tint = new Color(100, 100, 100);
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);

        float boundary = vertical ? grid.GetColumnBoundaryOffset(trackIndex) : grid.GetRowBoundaryOffset(trackIndex);
        var gridHalf = grid.HalfSize;

        HalfSizeOffset = vertical ? new Vector2(Thickness / 2f, gridHalf.Y) : new Vector2(gridHalf.X, Thickness / 2f);
        CenterOffset = vertical ? new Vector2(boundary, 0) : new Vector2(0, boundary);

        if (IsHighlighted || IsSelected)
            Game.Instance.Window.SetCursorStyle(vertical ? CursorStyle.SizeWE : CursorStyle.SizeNS);

        if (IsSelected && InputManager.IsInputHeld(Input.MouseLeft) && trackIndex > 0)
        {
            float delta = vertical ? InputManager.MouseDelta.X : InputManager.MouseDelta.Y;

            float current = vertical ? grid.ColumnSizes[trackIndex] : grid.RowSizes[trackIndex];
            float prev = vertical ? grid.ColumnSizes[trackIndex - 1] : grid.RowSizes[trackIndex - 1];

            // Grow one side by shrinking the other so total width is unchanged.
            if (vertical)
            {
                grid.SetColumnSize(trackIndex, current - delta);
                grid.SetColumnSize(trackIndex - 1, prev + delta);
            }
            else
            {
                grid.SetRowSize(trackIndex, current - delta);
                grid.SetRowSize(trackIndex - 1, prev + delta);
            }
        }
    }
}