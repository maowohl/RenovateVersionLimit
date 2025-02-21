#!/usr/bin/env dotnet-script
#load "common.csx"

#nullable enable

Log("Running test for project...");
CleanUp();

Log("Creating project...");
Run("dotnet", "dotnet new classlib -n TestProject -o testproject");

Log("Adding package to project...");
Run("dotnet", "dotnet add testproject/TestProject.csproj package System.Text.Json");

Log("Generating version limits...");
Run("dotnet", "run -- --file testproject/TestProject.csproj --output renovate-version-limits-project.json");

Log("Checking for expected output...");
AssertFileExists("renovate-version-limits-project.json");
AssertFileContains("renovate-version-limits-project.json", "System.Text.Json");
AssertFileContains("renovate-version-limits-project.json", "System.Memory");