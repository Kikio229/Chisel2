using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace DynEngine;
public class GameLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;

    public GameLoadContext(string gameDllPath) : base(isCollectible: true)
    {
        resolver = new AssemblyDependencyResolver(gameDllPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string path = resolver.ResolveAssemblyToPath(assemblyName);

        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }

        return null;
    }
}