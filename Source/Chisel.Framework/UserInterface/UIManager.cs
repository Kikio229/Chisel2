using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UI;
public class UIManager
{
    private UIRoot root = new UIRoot(default);
    private Rectangle rectangle;

    public void SetSize(int screenWidth, int screenHeight)
    {
        rectangle.X = 0;
        rectangle.Y = 0;
        rectangle.Width = screenWidth;
        rectangle.Height = screenHeight;
    }
    public void SetSize(Rectangle rect) => rectangle = rect;

    public void FrameUpdate(float delta)
    {
        root.CenterOffset.X = rectangle.Width / 2 + rectangle.X;
        root.CenterOffset.Y = rectangle.Height / 2 + rectangle.Y;

        root.HalfSizeOffset.X = rectangle.Width / 2;
        root.HalfSizeOffset.Y = rectangle.Height / 2;

        root.Update(delta);
    }
    public void FrameRender(float delta, SpriteBatch batch, Texture2D atlasTexture)
    {
        batch.Begin(Matrix.FromOrthographic(rectangle.X, rectangle.X + rectangle.Width,
                                             rectangle.Y + rectangle.Height, rectangle.Y, -10, 10));

        root.Render(delta, batch, atlasTexture);

        batch.End();
    }

    public void AddToRoot(UIObject obj)
    {
        root.Children.Add(obj);
        obj.Parent = root;
    }
}
