#!/usr/bin/env dotnet-script
#load "common.csx"

#nullable enable

Log("Running test for package...");
CleanUp();

Log("Generating version limits...");
Run("dotnet", "run -- --package System.Text.Json|5.0.1 --output renovate-version-limits-package.json");

Log("Checking for expected output...");
AssertFileExists("renovate-version-limits-package.json");
AssertFileContains("renovate-version-limits-package.json", "System.Text.Json");
AssertFileContains("renovate-version-limits-package.json", "System.Memory");