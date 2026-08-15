using Microsoft.Xna.Framework;

namespace Chisel.Framework.UI;
public class UIRoot : UIObject
{
    public UIRoot(UILayoutOptions options) : base(options)
    {
    }

    public override bool AllowNavigatingTo => false;
    public override bool AbsorbInputs => false;

    public override void OnHighlighted()
    {
    }

    public override void OnPrimaryClicked()
    {
    }

    public override void OnRender(float dt)
    {
    }

    public override void OnSecondaryClicked()
    {
    }

    public override void OnUnhighlighted()
    {
    }

    public override void OnUpdate(float dt)
    {
        var windowRef = Game.Instance?.Window;
        if (windowRef == null) return;

        var sizeX = windowRef.Resolution.W;
        var sizeY = windowRef.Resolution.H;

        CenterOffset.X = sizeX / 2f;
        CenterOffset.Y = sizeY / 2f;
        HalfExtents.X = sizeX / 2f;
        HalfExtents.Y = sizeY / 2f;
    }
}
