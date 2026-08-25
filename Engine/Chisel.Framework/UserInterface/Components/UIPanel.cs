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
        int cornerW = PanelRect.Width / 3;
        int cornerH = PanelRect.Height / 3;

        int srcMidW = PanelRect.Width - cornerW * 2;
        int srcMidH = PanelRect.Height - cornerH * 2;

        int[] srcX = { PanelRect.X, PanelRect.X + cornerW, PanelRect.X + PanelRect.Width - cornerW };
        int[] srcY = { PanelRect.Y, PanelRect.Y + cornerH, PanelRect.Y + PanelRect.Height - cornerH };
        int[] srcW = { cornerW, srcMidW, cornerW };
        int[] srcH = { cornerH, srcMidH, cornerH };

        int fullW = (int)(HalfSize.X * 2);
        int fullH = (int)(HalfSize.Y * 2);

        int destCornerW = Math.Min(cornerW, fullW / 2);
        int destCornerH = Math.Min(cornerH, fullH / 2);

        int[] dstW = { destCornerW, fullW - destCornerW * 2, destCornerW };
        int[] dstH = { destCornerH, fullH - destCornerH * 2, destCornerH };

        int xpos = (int)(Position.X - HalfSize.X);
        for (int x = 0; x < 3; x++)
        {
            int ypos = (int)(Position.Y - HalfSize.Y);
            for (int y = 0; y < 3; y++)
            {
                batch.Draw(atlasTexture, new(xpos, ypos), new(dstW[x], dstH[y]), Tint,
                    new(srcX[x], srcY[y], srcW[x], srcH[y]));
                ypos += dstH[y];
            }
            xpos += dstW[x];
        }

        //batch.SetClip(new((int)(Position.X - HalfSize.X), (int)(Position.Y - HalfSize.Y), fullW, fullH));
    }
    public override void OnUpdate(float dt)
    {
    }
}