using CommandLine;
using Maowohl.RenovateVersionLimit.Classes;
using Maowohl.RenovateVersionLimit.Extensions;

namespace Maowohl.RenovateVersionLimit;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(RunWithOptions);
    }

    private static async Task RunWithOptions(Options opts)
    {
        List<Dependency> packages =
        [
            // TODO: parse namespace parameters

            // parse package parameters
            .. opts.DependenciesFromPackageNames(),
            // parse solution parameters
            .. opts.DependenciesFromSolutionFile(),
            // parse project parameters
            .. opts.DependenciesFromProjectFiles(),
        ];

        // resolve dependencies recursively
        var resolvedPackages = await packages.WalkAsync(opts.MaxDepth);

        // deduplicate
        resolvedPackages = resolvedPackages.Deduplicate();

        // generate file
        var renovateFile = RenovateFile.FromDependencyList(resolvedPackages);
        renovateFile.SaveToFile(opts.OutputFileName);
    }
}