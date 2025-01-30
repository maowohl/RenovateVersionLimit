// DOCS: https://learn.microsoft.com/en-us/nuget/reference/nuget-client-sdk

// pegar lista de projetos daqui
// https://github.com/abpframework/abp/blob/dev/nupkg/common.ps1
// ou todos os arquivos .csproj das pastas modules e framework

// for now resolve the dependencies onf the package Volo.Abp only
//var packageIdentity = new PackageIdentity("Volo.Abp", NuGetVersion.Parse("1.0.0"));

using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

// parametros(utilizar CommandLineParser https://www.nuget.org/packages/CommandLineParser/)
// - maxDepth (setar um numero grandão se quiser que seja "infinito")
// - packageVersionSeparator (default|)
// - package(with version) package|1.0.0 
// - project
// - outputFile (return something or another if changed or not so it can be scripted)



int MaxDepthParameter = 99;

ILogger logger = NullLogger.Instance;
CancellationToken cancellationToken = CancellationToken.None;

SourceCacheContext cache = new SourceCacheContext();
SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
DependencyInfoResource dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();

/*

List<KeyValuePair<string, string>> packages = new List<KeyValuePair<string, string>>();
packages.Add(new KeyValuePair<string, string>("Volo.Abp", "1.0.0"));

int i = 0; // resolved packages count
while (i < packages.Count)
{
    var result = await dependencyInfoResource.ResolvePackage(
        new PackageIdentity(packages[i].Key, NuGetVersion.Parse(packages[i].Value)),
        NuGetFramework.AnyFramework,
        cache,
        logger,
        cancellationToken
    );

    foreach (var dep in result.Dependencies)
    {
        if (!packages.Any(i => i.Key == dep.Id))
        {
            packages.Add(new KeyValuePair<string, string>(dep.Id, dep.VersionRange.MinVersion!.ToString()));
        }
    }
    i++; // increase packages resolved
}

foreach (var package in packages)
{
    Console.WriteLine($"{package.Key} {package.Value}");
}
Console.WriteLine($"{packages.Count} dependencias resolvidas");
//*/

// tentativa de lógica de resolução
///*
List<Dependency> packages = new();

// add packages to be queried
packages.Add(new Dependency
{
    Name = "Volo.Abp",
    Version = "1.0.0",
    Level = 0 // level 0 are packages passed as initial parameters
});

packages.Add(new Dependency
{
    Name = "Volo.Abp",
    Version = "1.0.2",
    Level = 0 // level 0 are packages passed as initial parameters
});

async Task<List<Dependency>> WalkDependenciesAsync(int packageLevel, List<Dependency> packages, int maxDepth = 0)
{
    if (!(packageLevel < maxDepth))
    {
        return packages;
    }

    // usar um for de trás pra frente aqui?
    var newPackages = new List<Dependency>();

    for (int i = packages.Count-1; i > -1; i--)
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

    return await WalkDependenciesAsync(++packageLevel, packages, maxDepth);
}

List<Dependency> DedupDependencies(List<Dependency> packages)
{
    var result = new List<Dependency>();
    // the rule is to prefer newer out of the bunch
    packages.Sort(ComparePackagesByNameAndVersion);
    foreach (var item in packages)
    {
        if (!result.Any(p => p.Name == item.Name))
            result.Add(item);
    }

    return result;
}

var resolvedPackages = await WalkDependenciesAsync(0, packages, MaxDepthParameter);
resolvedPackages = DedupDependencies(resolvedPackages);

static int ComparePackagesByNameAndVersion(Dependency x, Dependency y)
{
    //  0 = equal
    // -1 = y is greater than x
    //  1 = x is greater than y?

    // first order by name, then order by semver desc
    int nameOrder  = x.Name.CompareTo(y.Name);
    if (nameOrder != 0)
        return nameOrder;

    var xVersion = SemanticVersion.Parse(x.Version);
    var yVersion = SemanticVersion.Parse(y.Version);

    var Comparer = VersionComparer.Get(VersionComparison.Version);

    return Comparer.Compare(yVersion, xVersion); // desc because we want bigger
}


foreach (var package in resolvedPackages)
{
    Console.WriteLine($"{package.Name} {package.Version} {package.Level}");
}
Console.WriteLine($"{resolvedPackages.Count()} dependencias resolvidas");


public class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
}
//*/