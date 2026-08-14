using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
namespace Chisel.Resource.Builder;
static class AssetsConfigLocator
{
    public static string Find(string projectDirectory)
    {
        string path = Path.Combine(projectDirectory, "assets.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("assets.json not found next to project", path);
        }

        return path;
    }
}