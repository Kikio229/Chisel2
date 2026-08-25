using System;

namespace Chisel.Framework.UI;

[Flags]
public enum UIAnchor
{
    Center = 0,
    Top = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Bottom = 1 << 4,
    FillH = 1 << 5,
    FillV = 1 << 6,
}