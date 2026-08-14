using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
namespace Chisel.Resource.Builder;
interface IAssetHandler
{
    byte[] Convert(string sourcePath);
    string OutputExtension { get; }
}
// Simplest possible handler. Literally just copies the bytes over.
class RawHandler : IAssetHandler
{
    public string OutputExtension => null;

    public byte[] Convert(string sourcePath)
    {
        return File.ReadAllBytes(sourcePath);
    }
}