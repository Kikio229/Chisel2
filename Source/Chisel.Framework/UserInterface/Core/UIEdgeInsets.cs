namespace Chisel.Framework.UI;

public struct UIEdgeInsets
{
    public float Left, Top, Right, Bottom;

    public UIEdgeInsets(float all)
    {
        Left = Top = Right = Bottom = all;
    }

    public UIEdgeInsets(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public UIEdgeInsets(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}