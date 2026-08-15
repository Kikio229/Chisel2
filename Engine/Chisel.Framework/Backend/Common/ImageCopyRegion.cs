using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;
public struct ImageCopyRegion
{
    public ulong BufferOffset;
    public int DestX;
    public int DestY;
    public uint Width;
    public uint Height;
    public uint MipLevel;
}