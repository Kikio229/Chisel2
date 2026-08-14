using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
[Flags]
public enum GraphicsClearFlags
{
    Color = 1 << 0,
    Depth = 1 << 1,
    Stencil = 1 << 2,
}
