using Chisel.Resource.Builder;
using System;
using System.Linq;

namespace Chisel.ResourceBuilder;

internal class Program
{
    static int Main(string[] args)
    {
        if(args.Length < 2)
        {
            Console.Error.WriteLine("usage: builder <projectDirectory> <outputDirectory>");
            return 1;
        }

        string projectDir = args[0];
        string outputDir = args[1];
        bool pack = args.Contains("--pack");

        string configPath = AssetsConfigLocator.Find(projectDir);
        ResolvedBuildConfig resolvedConfig = BuildConfigLoader.Resolve(configPath);

        BuildOptions options = new BuildOptions
        {
            Pack = pack,
            UnpackedPaths = resolvedConfig.UnpackedPaths,
        };

        AssetWalker.Build(resolvedConfig.ContentDirectories, outputDir, options);

        return 0;
    }
}
