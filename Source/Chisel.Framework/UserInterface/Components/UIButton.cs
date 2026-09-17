using Chisel.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UI;

public enum ButtonStyle
{
    Default,
    FlatTop,
}

internal class UIButton : UIPanel
{

    public ButtonStyle Style;

    public event Action OnClicked;
    public UIObject Content;

    public UIButton(UILayoutOptions options) : base(options)
    {
        Content = new UIPanel(options);

        AddChild(Content);
    }

    public override Rectangle PanelRect => (IsSelected ? new Rectangle(192 + 32, 32, 32, 32) : 
                                           (IsHighlighted ? new Rectangle(192, 32 + 32, 32, 32) : new Rectangle(192, 32, 32, 32))) + GetOffset();

    private Rectangle GetOffset()
    {
        return Style switch
        {
            ButtonStyle.FlatTop => new Rectangle(64,0,0,0),
            _ => new Rectangle(0,0,0,0),
        };
    }
    public override void OnPrimaryClicked()
    {
        OnClicked();
    }
    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        base.OnRender(dt, batch, atlasTexture);
        RenderPanel(dt, batch, atlasTexture, Color.White, PanelRect + new Rectangle(0,64,0,0));
    }
}
