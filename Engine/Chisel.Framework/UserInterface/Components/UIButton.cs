using Chisel.Framework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UserInterface.Components;
internal class UIButton : UIPanel
{
    public UIButton(UILayoutOptions options) : base(options)
    {
    }

    public override Rectangle PanelRect => new(192,32,32,32);
}
