using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UI;
public class UIPanel : UIObject
{
    public UIPanel(UILayoutOptions options) : base(options)
    {
    }

    public override bool AllowNavigatingTo => false;

    public override bool AbsorbInputs => true;

    public virtual Rectangle PanelRect => new Rectangle(96, 0, 96, 96);

    public override void OnHighlighted()
    {
    }
    public override void OnUnhighlighted()
    {
    }
    public override void OnSecondaryClicked()
    {
    }
    public override void OnPrimaryClicked()
    {
    }

    public override void OnRender(float dt, SpriteBatch batch, Texture2D atlasTexture)
    {
        int width = PanelRect.Width / 3;
        int height = PanelRect.Height / 3;

        int stretchPanelW = (int)(HalfExtents.X * 2 - width * 2);
        int stretchPanelH = (int)(HalfExtents.Y * 2 - height * 2);

        int xpos = (int)(Position.X - HalfExtents.X);

        for (int x = 0; x < 3; x++)
        {
            int ypos = (int)(Position.Y - HalfExtents.Y);

            int curw = x == 1 ? stretchPanelW : width;

            for (int y = 0; y < 3; y++)
            {
                int curh = y == 1 ? stretchPanelH : height;

                batch.Draw(atlasTexture, new(xpos, ypos), new(curw, curh), Tint, new(width * x + PanelRect.X, height * y + PanelRect.Y, width, height));

                ypos += curh;
            }

            xpos += curw;
        }
    }
    public override void OnUpdate(float dt)
    {
    }
}