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

ILogger logger = NullLogger.Instance;
CancellationToken cancellationToken = CancellationToken.None;

SourceCacheContext cache = new SourceCacheContext();
SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
DependencyInfoResource dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();


List<KeyValuePair<string, string>> packages = new List<KeyValuePair<string, string>>();
packages.Add(new KeyValuePair<string, string>("Volo.Abp", "1.0.0"));

int i = 0;
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
    i++;
}

foreach (var package in packages)
{
    Console.WriteLine($"{package.Key} {package.Value}");
}
Console.WriteLine($"{packages.Count} dependencias resolvidas");