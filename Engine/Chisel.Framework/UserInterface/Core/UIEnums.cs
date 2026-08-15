using System;

namespace Chisel.Framework.UI;

[Flags]
public enum UIAnchor
{
    Center = 0,
    Top = 1 << 0,
    Left = 1 << 1,
    Right = 1 << 2,
    Bottom = 1 << 2,
}