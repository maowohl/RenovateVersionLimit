using Maowohl.RenovateVersionLimit.Classes;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Maowohl.RenovateVersionLimit.Extensions;

public static class DependencyExtensions
{
    public static async Task<List<Dependency>> WalkAsync(this List<Dependency> dependencies, int maxDepth = 0)
    {
        ISettings Settings = NuGet.Configuration.Settings.LoadDefaultSettings(
                        Directory.GetCurrentDirectory(),
                        configFileName: null,
                        machineWideSettings: null);

        IPackageSourceProvider SourceProvider = new PackageSourceProvider(Settings);
        List<PackageSource> packageSources = SourceProvider.LoadPackageSources()
                .Where(p => p.IsEnabled)
                .ToList();
        SourceCacheContext cacheContext = new SourceCacheContext();

        return await WalkDependenciesAsync(packageSources, cacheContext, 0, dependencies, maxDepth);
    }

    public static List<Dependency> Deduplicate(this IEnumerable<Dependency> dependencies)
    {
        // Prefer newer versions by sorting first using semantic versioning
        var sortedPackages = dependencies.OrderBy(p => p.Name)
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

    private static async Task<List<Dependency>> WalkDependenciesAsync(
        List<PackageSource> packageSources,
        SourceCacheContext cacheContext,
        int packageLevel,
        List<Dependency> packages,
        int maxDepth)
    {
        if (!(packageLevel < maxDepth) || !packages.Any(p => p.Level >= packageLevel))
        {
            return packages;
        }

        ILogger logger = NullLogger.Instance;
        CancellationToken cancellationToken = CancellationToken.None;

        for (int i = packages.Count - 1; i > -1; i--)
        {
            var package = packages[i];

            if (package.Level != packageLevel)
                continue;

            SourcePackageDependencyInfo? result = null;

            foreach (PackageSource source in packageSources)
            {
                if (result is not null)
                    break; // found

                SourceRepository repository = Repository.Factory.GetCoreV3(source);
                DependencyInfoResource dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();

                if (dependencyInfoResource is null)
                    continue;

                result = await dependencyInfoResource.ResolvePackage(
                    new PackageIdentity(package.Name, NuGetVersion.Parse(package.Version)),
                    NuGetFramework.AnyFramework,
                    cacheContext,
                    logger,
                    cancellationToken
                );
            }

            if (result is null)
                throw new Exception($"Could not find package {package.Name} in any of the sources");

            foreach (var dependency in result.Dependencies)
            {
                if (packages.Any(p => p.Name == dependency.Id && p.Version == dependency.VersionRange.MinVersion!.ToString()))
                    continue; // skip to avoid loop

                packages.Add(new Dependency
                {
                    Name = dependency.Id,
                    Version = dependency.VersionRange.MinVersion!.ToString(),
                    Level = packageLevel + 1
                });
            }
        }

        // handle next level
        return await WalkDependenciesAsync(
            packageSources,
            cacheContext,
            ++packageLevel,
            packages,
            maxDepth);
    }
}