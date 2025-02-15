using Maowohl.RenovateVersionLimit.Classes;
using Microsoft.Build.Construction;

namespace Maowohl.RenovateVersionLimit.Extensions;

public static class OptionsExtensions
{
    // TODO: parse namespace parameters

    public static IEnumerable<Dependency> DependenciesFromPackageNames(this Options options)
    {
        foreach (var packageName in options.PackageNames.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            var packageParts = packageName.Split(options.VersionSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (packageParts.Length == 2)
            {
                yield return new Dependency
                {
                    Name = packageParts[0],
                    Version = packageParts[1],
                    Level = 0
                };
            }
            else
            {
                throw new ArgumentException($"Invalid package name or version: {packageName}. Expected format: 'packageName{options.VersionSeparator}version'");
            }
        }
    }

    public static IEnumerable<Dependency> DependenciesFromSolutionFile(this Options options)
    {
        var projectOptions = new Options();
        foreach (var solutionFile in options.SolutionFiles.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            SolutionFile solution;
            try
            {
                solution = SolutionFile.Parse(solutionFile);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid solution file: {solutionFile}. Error: {ex.Message}");
            }

            // parse projects
            foreach (var project in solution.ProjectsInOrder)
            {
                projectOptions.ProjectFiles.Append(project.RelativePath);
            }
        }
        // reuse DependenciesFromProjectFiles
        return projectOptions.DependenciesFromProjectFiles();
    }

    public static IEnumerable<Dependency> DependenciesFromProjectFiles(this Options options)
    {
        foreach (var project in options.ProjectFiles.Where(s => !string.IsNullOrWhiteSpace(s)))
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
                yield return new Dependency
                {
                    Name = packageName,
                    Version = version,
                    Level = 0
                };
            }
        }
    }
}