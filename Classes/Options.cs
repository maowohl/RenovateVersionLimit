using CommandLine;

namespace Maowohl.RenovateVersionLimit.Classes;

public class Options
{
    [Option('d', "depth", Required = false, HelpText = "Maximum depth of the search.")]
    public int MaxDepth { get; set; } = int.MaxValue;

    [Option('s', "separator", Required = false, HelpText = "Version separator to use with the package option.")]
    public string VersionSeparator { get; set; } = "|";

    [Option('p', "package", Separator = ' ', Required = false, HelpText = "Package name to search for. Formatted as <name><separator><version>.")]
    public IEnumerable<string> PackageNames { get; set; } = new List<string>();

    [Option('s', "solution", Separator = ' ', Required = false, HelpText = "Solution(.sln) to search for projects in.")]
    public IEnumerable<string> SolutionFiles { get; set; } = new List<string>();

    [Option('f', "file", Separator = ' ', Required = false, HelpText = "Project file(.csproj) for parsing dependencies.")]
    public IEnumerable<string> ProjectFiles { get; set; } = new List<string>();

    [Option('o', "output", Required = false, HelpText = "Output file name for the generated renovate config.")]
    public string OutputFileName { get; set; } = "renovate-version-limits.json";
}
