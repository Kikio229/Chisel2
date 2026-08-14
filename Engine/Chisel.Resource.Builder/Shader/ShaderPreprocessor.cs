using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chisel.Resource.Builder;

static class ShaderPreprocessor
{
    static readonly Regex LibraryDirective = new Regex(@"^\s*#library\s+(\S+)\s*(?://.*)?$", RegexOptions.Multiline);

    public static string ExpandLibraries(string source)
    {
        return ExpandLibraries(source, new HashSet<string>());
    }

    static string ExpandLibraries(string source, HashSet<string> visited)
    {
        return LibraryDirective.Replace(source, match =>
        {
            string name = match.Groups[1].Value;

            if (!visited.Add(name))
            {
                throw new InvalidOperationException("Circular #library reference detected: '" + name + "'.");
            }

            string libraryText = ShaderLibraryRegistry.Resolve(name);
            string expanded = ExpandLibraries(libraryText, visited);

            visited.Remove(name);
            return expanded;
        });
    }
}