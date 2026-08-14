using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Resource;
public static class ContentPath
{
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }
}