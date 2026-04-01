param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$TargetPath,

    [string]$OutputRoot,

    [switch]$NoRestore,

    [switch]$Clean,

    [switch]$UseDefaultOutput
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($TargetPath)) {
    $solution = Get-ChildItem -Path $PSScriptRoot -Filter *.sln | Select-Object -First 1

    if ($solution) {
        $TargetPath = $solution.FullName
    }
    else {
        $project = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter *.csproj | Select-Object -First 1

        if (-not $project) {
            throw "No .sln or .csproj file was found under $PSScriptRoot."
        }

        $TargetPath = $project.FullName
    }
}
elseif (-not [System.IO.Path]::IsPathRooted($TargetPath)) {
    $TargetPath = Join-Path $PSScriptRoot $TargetPath
}

if (-not (Test-Path -LiteralPath $TargetPath)) {
    throw "Build target not found: $TargetPath"
}

$resolvedTarget = (Resolve-Path -LiteralPath $TargetPath).Path

if (-not $UseDefaultOutput) {
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = Join-Path $PSScriptRoot "artifacts"
    }
    elseif (-not [System.IO.Path]::IsPathRooted($OutputRoot)) {
        $OutputRoot = Join-Path $PSScriptRoot $OutputRoot
    }

    $OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    $baseOutputPath = [System.IO.Path]::GetFullPath((Join-Path $OutputRoot "bin")) + "\"
    $baseIntermediateOutputPath = [System.IO.Path]::GetFullPath((Join-Path $OutputRoot "obj")) + "\"

    New-Item -ItemType Directory -Force -Path $baseOutputPath | Out-Null
    New-Item -ItemType Directory -Force -Path $baseIntermediateOutputPath | Out-Null
}

$cleanArgs = @(
    "clean",
    $resolvedTarget,
    "-c",
    $Configuration
)

$buildArgs = @(
    "build",
    $resolvedTarget,
    "-c",
    $Configuration
)

if (-not $UseDefaultOutput) {
    $cleanArgs += "-p:BaseOutputPath=$baseOutputPath"
    $cleanArgs += "-p:BaseIntermediateOutputPath=$baseIntermediateOutputPath"
    $cleanArgs += "-p:CustomBuildArtifactsRoot=$OutputRoot"
    $buildArgs += "-p:BaseOutputPath=$baseOutputPath"
    $buildArgs += "-p:BaseIntermediateOutputPath=$baseIntermediateOutputPath"
    $buildArgs += "-p:CustomBuildArtifactsRoot=$OutputRoot"
}

if ($NoRestore) {
    $cleanArgs += "--no-restore"
    $buildArgs += "--no-restore"
}

if ($Clean) {
    Write-Host "Cleaning: $resolvedTarget ($Configuration)"
    & dotnet @cleanArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet clean failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Building: $resolvedTarget ($Configuration)"

if (-not $UseDefaultOutput) {
    Write-Host "Output root: $OutputRoot"
}

& dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

Write-Host "Build completed successfully."
