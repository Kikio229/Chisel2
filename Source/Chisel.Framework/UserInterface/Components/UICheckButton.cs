using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework.UI;

public class UICheckButton : UIPanel
{
    public bool Checked;
    public event Action<bool> OnChanged;

    public UICheckButton(UILayoutOptions options) : base(options)
    {
    }

    public override Rectangle PanelRect => Checked
        ? (IsHighlighted ? new Rectangle(224, 96, 32, 32) : new Rectangle(224, 64, 32, 32))
        : (IsHighlighted ? new Rectangle(192, 96, 32, 32) : new Rectangle(192, 64, 32, 32));

    public override void OnPrimaryClicked()
    {
        Checked = !Checked;
        OnChanged?.Invoke(Checked);
    }
}