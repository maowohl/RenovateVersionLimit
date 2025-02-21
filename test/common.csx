#!/usr/bin/env dotnet-script
#load "RenovateFile.csx"
using System.Data;
using System.Diagnostics;
using System.Text.Json;

#nullable enable

# region methods

void CleanUp()
{
    // remove testsolution folder if exists
    if (Directory.Exists("testsolution"))
        Directory.Delete("testsolution", true);

    // remove testproject folder if exists
    if (Directory.Exists("testproject"))
        Directory.Delete("testproject", true);

    // remove renovate-version-limits-namespace.json if exists
    if (File.Exists("renovate-version-limits-namespace.json"))
        File.Delete("renovate-version-limits-namespace.json");

    // remove renovate-version-limits-package.json if exists
    if (File.Exists("renovate-version-limits-package.json"))
        File.Delete("renovate-version-limits-package.json");

    // remove renovate-version-limits-solution.json if exists
    if (File.Exists("renovate-version-limits-solution.json"))
        File.Delete("renovate-version-limits-solution.json");

    // remove renovate-version-limits-project.json if exists
    if (File.Exists("renovate-version-limits-project.json"))
        File.Delete("renovate-version-limits-project.json");
}

void Run(string fileName = "dotnet", string arguments = "")
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        }
    };
    process.OutputDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log($"{e.Data}");
    };
    process.ErrorDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
            Log($"{e.Data}", "error");
    };
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
    var exitCode = process.ExitCode;
    if (exitCode != 0)
    {
        Log($"return code {exitCode} from {fileName} {arguments}", "error");
        Environment.Exit(1);
    }
}

void AssertFileExists(string path)
{
    if (!File.Exists(path))
    {
        Log($"file not found: {path}", "error");
        Environment.Exit(1);
    }
    Log($"{path} exists");
}

void AssertFileContains(string path, string package, string? allowedVersions = null)
{
    // parse RenovateFile and check if the file contains the expected package and optionally version
    try
    {
        var renovateFile = JsonSerializer.Deserialize<RenovateFile>(File.ReadAllText(path)) ?? throw new Exception("RenovateFile is null");
        var foundPackage = renovateFile.PackageRules.FirstOrDefault(p => p.MatchPackageNames.Any(n => n == package) && (allowedVersions is null || p.AllowedVersions == allowedVersions));

        if (foundPackage is null)
        {
            Log($"file does not contain expected package: {package} {allowedVersions}", "error");
        }
        else
        {
            Log($"file contains expected package: {package} {foundPackage.AllowedVersions}");
        }
    }
    catch (Exception ex)
    {
        Log($"failed to parse RenovateFile: {ex.Message}", "error");
        Environment.Exit(1);
    }
}


void Log(string message, string prefix = "info")
{
    Console.WriteLine($"{prefix}: {message}");
}

# endregion methods
