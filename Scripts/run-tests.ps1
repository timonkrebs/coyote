# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("net10.0", "net8.0", "net462")]
    [string]$framework = "net10.0",
    [ValidateSet("all", "runtime", "rewriting", "testing", "actors", "actors-testing", "tools")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal",
    [switch]$cli,
    [switch]$ci
)

Import-Module $PSScriptRoot/common.psm1 -Force

$all_frameworks = (Get-Variable "framework").Attributes.ValidValues
$targets = [ordered]@{
    "runtime" = "Tests.Runtime"
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
    "tools" = "Tests.Tools"
}

# Find that paths to the installed .NET runtime.
$dotnet = "dotnet"
$dotnet_runtime_path = FindDotNetRuntimePath -dotnet $dotnet -runtime "NETCore"
$aspnet_runtime_path = FindDotNetRuntimePath -dotnet $dotnet -runtime "AspNetCore"
$runtime_version = FindDotNetRuntimeVersion -dotnet_runtime_path $dotnet_runtime_path

# Restore the local ilverify tool.
# NOTE: do NOT clear the NuGet caches here ('dotnet nuget locals all --clear'):
# this repository redirects the NuGet global packages folder to ./packages
# (see NuGet.config), so clearing it after the build deletes the restored test
# packages, which silently turns every 'dotnet test --no-build' invocation
# below into a no-op that executes zero tests but still exits successfully.
&dotnet tool restore
&dotnet tool list
$ilverify = "dotnet ilverify"

[System.Environment]::SetEnvironmentVariable('COYOTE_CLI_TELEMETRY_OPTOUT', '1')

# Publish a results table to the GitHub Actions job summary, if available.
if ($env:GITHUB_STEP_SUMMARY) {
    "### Coyote test results`n`n| Project | Framework | Total | Passed | Failed | Skipped |`n|---|---|---|---|---|---|" | `
        Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Encoding utf8 -Append
}

# Run all enabled tests.
Write-Comment -text "Running the Coyote tests." -color "blue"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    $frameworks = Get-ChildItem -Path "$PSScriptRoot/../Tests/$($kvp.Value)/bin" | `
        Where-Object Name -CIn $all_frameworks | Select-Object -expand Name
    foreach ($f in $frameworks) {
        if ((-not $ci.IsPresent) -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
        if ($f -eq "net10.0") {
            $AssemblyName = GetAssemblyName($target)
            $command = [IO.Path]::Combine($PSScriptRoot, "..", "Tests", $($kvp.Value), "bin", "net10.0", "$AssemblyName.dll")
            $command = $command + ' -r "' + [IO.Path]::Combine( `
                $PSScriptRoot, "..", "Tests", $($kvp.Value), "bin", "net10.0", "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($PSScriptRoot, "..", "bin", "net10.0", "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($dotnet_runtime_path, $runtime_version, "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($aspnet_runtime_path, $runtime_version, "*.dll") + '"'
            Invoke-ToolCommand -tool $ilverify -cmd $command -error_msg "found corrupted assembly rewriting"
        }

        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target `
            -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

if ($cli.IsPresent -and $IsWindows) {
    Write-Comment -text "Running the Coyote CLI NuGet tool installation test." -color "blue"

    $ErrorActionPreference = 'Stop'
    $temp_path = "bin/temp"
    $cli_tool_path = "$PSScriptRoot/../$temp_path"
    New-Item -Path $cli_tool_path -ItemType Directory -Force | out-null
    if (Test-Path $cli_tool_path/coyote.exe) {
        Write-Comment -text "Uninstalling the Microsoft.Coyote.CLI package."
        dotnet tool uninstall Microsoft.Coyote.CLI --tool-path $temp_path
    }

    Write-Comment -text "Installing the Microsoft.Coyote.CLI package."
    dotnet tool install --add-source $PSScriptRoot/../bin/nuget Microsoft.Coyote.CLI --no-cache --tool-path $temp_path

    $help = (& "$cli_tool_path/coyote" -?) -join '\n'
    Remove-Item $cli_tool_path -Recurse
    if (!$help.Contains("coyote [command] [options]")) {
        Write-Error "### Unexpected output from coyote command"
        Write-Error $help
        Exit 1
    }
}

Write-Comment -text "Done." -color "green"
