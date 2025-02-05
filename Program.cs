using CommandLine;
using Microsoft.Build.Construction;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using renovate_nuget_version_limit.Classes;

namespace renovate_nuget_version_limit;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(RunOptions);
    }

    private static async Task RunOptions(Options opts)
    {
        List<Dependency> packages = new();
        // parse package parameters
        foreach (var packageName in opts.PackageNames.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var packageParts = packageName.Split(opts.VersionSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (packageParts.Length == 2)
            {
                packages.Add(new Dependency
                {
                    Name = packageParts[0],
                    Version = packageParts[1],
                    Level = 0
                });
            }
            else
            {
                throw new ArgumentException($"Invalid package name or version: {packageName}. Expected format: 'packageName{opts.VersionSeparator}version'");
            }
        }

        // parse project parameters
        foreach (var project in opts.ProjectFiles.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            ProjectRootElement projectRootElement;
            try
            {
                projectRootElement = ProjectRootElement.Open(project);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid project file: {project}. Error: {ex.Message}");
            }

            // parse dependencies
            foreach (var package in projectRootElement.Items.Where(i => i.ItemType == "PackageReference"))
            {
                var packageName = package.Include;
                var versionElement = (ProjectMetadataElement?)package.Children.FirstOrDefault(c => c.ElementName == "Version");
                if (versionElement == null)
                {
                    throw new ArgumentException($"Invalid project file: {project}. Package '{packageName}' does not have a version specified.");
                }
                var version = versionElement.Value;
                packages.Add(new Dependency
                {
                    Name = packageName,
                    Version = version,
                    Level = 0
                });
            }
        }

        ILogger logger = NullLogger.Instance;
        CancellationToken cancellationToken = CancellationToken.None;

        SourceCacheContext cache = new SourceCacheContext();
        SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        DependencyInfoResource dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();

        async Task<List<Dependency>> WalkDependenciesAsync(int packageLevel, List<Dependency> packages, int maxDepth = 0)
        {
            if (!(packageLevel < maxDepth) || !packages.Any(p => p.Level >= packageLevel))
            {
                return packages;
            }

            for (int i = packages.Count - 1; i > -1; i--)
            {
                var package = packages[i];

                if (package.Level != packageLevel)
                    continue;

                var result = await dependencyInfoResource.ResolvePackage(
                    new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version)),
                    NuGetFramework.AnyFramework,
                    cache,
                    logger,
                    cancellationToken
                );

                foreach (var dependency in result.Dependencies)
                {
                    packages.Add(new Dependency
                    {
                        Name = dependency.Id,
                        Version = dependency.VersionRange.MinVersion!.ToString(),
                        Level = packageLevel + 1
                    });
                }
            }

            // handle next level
            return await WalkDependenciesAsync(++packageLevel, packages, maxDepth);
        }

        List<Dependency> DedupDependencies(List<Dependency> packages)
        {
            // Prefer newer versions by sorting first using semantic versioning
            var sortedPackages = packages.OrderBy(p => p.Name)
                                         .ThenByDescending(p => SemanticVersion.Parse(p.Version), VersionComparer.Get(VersionComparison.Version))
                                         .ToList();

            // Use LINQ to deduplicate while preserving order
            var result = new List<Dependency>();
            foreach (var item in sortedPackages)
            {
                if (!result.Any(p => p.Name == item.Name))
                    result.Add(item);
            }

            return result;
        }

        var resolvedPackages = await WalkDependenciesAsync(0, packages, opts.MaxDepth);
        resolvedPackages = DedupDependencies(resolvedPackages);

        var renovateFile = new RenovateFile();

        foreach (var package in resolvedPackages)
        {
            renovateFile.AddPackage(package.Name, package.Version);
        }

        renovateFile.SaveToFile(opts.OutputFileName);
    }
}