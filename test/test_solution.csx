#!/usr/bin/env dotnet-script
#load "common.csx"

#nullable enable

Log("Running integration tests...");
CleanUp();

Log("Creating solution...");
Run("dotnet", "dotnet new solution -n TestSolution -o testsolution");

Log("Creating project and adding to solution...");
Run("dotnet", "dotnet new classlib -n TestProject -o testsolution/TestProject");
Run("dotnet", "dotnet sln testsolution/TestSolution.sln add testsolution/TestProject/TestProject.csproj");

Log("Adding package to project...");
Run("dotnet", "dotnet add testsolution/TestProject/TestProject.csproj package System.Text.Json");

Log("Generating version limits...");
Run("dotnet", "run -- --solution testsolution/TestSolution.sln --output renovate-version-limits-solution.json");

Log("Checking for expected output...");
AssertFileExists("renovate-version-limits-solution.json");
AssertFileContains("renovate-version-limits-solution.json", "System.Text.Json");
AssertFileContains("renovate-version-limits-solution.json", "System.Memory");