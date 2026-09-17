namespace Chisel.Framework.UI;

public class UITextBlock : UIObject
{
    public string Text = string.Empty;
    public Color TextColor = Color.White;
    public float FontSize = 20f;

    public UITextBlock(UILayoutOptions options) : base(options)
    {
    }

    public override bool AllowNavigatingTo => false;
    public override bool AbsorbInputs => false;

    public override void OnUpdate(float dt)
    {
    }

    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        var pos = Position;
        Vector2 measured = batch.MeasureText(Text, (int)FontSize);

        float x;

        if (Anchor.HasFlag(UIAnchor.Left))
        {
            x = pos.X;
        }
        else if (Anchor.HasFlag(UIAnchor.Right))
        {
            x = pos.X - measured.X;
        }
        else
        {
            x = pos.X - measured.X / 2f;
        }

        float y;

        if (Anchor.HasFlag(UIAnchor.Top))
        {
            y = pos.Y;
        }
        else if (Anchor.HasFlag(UIAnchor.Bottom))
        {
            y = pos.Y - measured.Y;
        }
        else
        {
            y = pos.Y - measured.Y / 2f;
        }

        batch.DrawString(Text, (int)FontSize, new Vector2(x, y), TextColor);
    }

    public override void OnHighlighted()
    {
    }
    public override void OnUnhighlighted()
    {
    }
    public override void OnPrimaryClicked()
    {
    }
    public override void OnSecondaryClicked()
    {
    }
}