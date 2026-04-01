param(
    [string]$ProjectName = (Split-Path -Leaf $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $baseUri = New-Object System.Uri((Resolve-Path $BasePath).Path.TrimEnd('\') + '\')
    $targetUri = New-Object System.Uri((Resolve-Path $TargetPath).Path)

    return [System.Uri]::UnescapeDataString(
        $baseUri.MakeRelativeUri($targetUri).ToString().Replace('/', '\')
    )
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($backupRoot)) {
    $backupRoot = $PSScriptRoot
}

$archiveName = "{0}_{1}.zip" -f $ProjectName, $timestamp
$archivePath = Join-Path $backupRoot $archiveName

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$excludeDirectories = @(
    "artifacts",
    "bin",
    "obj",
    ".vs",
    "backups"
)

$sourceFiles = Get-ChildItem -Path $PSScriptRoot -Recurse -File | Where-Object {
    $relativePath = Get-RelativePath -BasePath $PSScriptRoot -TargetPath $_.FullName
    $segments = $relativePath -split "[\\/]"

    foreach ($segment in $segments) {
        if ($excludeDirectories -contains $segment) {
            return $false
        }
    }

    return $true
}

$zipFile = [System.IO.File]::Open($archivePath, [System.IO.FileMode]::CreateNew)

try {
    $zipArchive = New-Object System.IO.Compression.ZipArchive($zipFile, [System.IO.Compression.ZipArchiveMode]::Create, $false)

    foreach ($file in $sourceFiles) {
        $entryName = Get-RelativePath -BasePath $PSScriptRoot -TargetPath $file.FullName
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zipArchive, $file.FullName, $entryName) | Out-Null
    }
}
finally {
    if ($zipArchive) {
        $zipArchive.Dispose()
    }

    $zipFile.Dispose()
}

Write-Host "Backup created: $archivePath"
