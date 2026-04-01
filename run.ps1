param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

if ($Configuration -notin @("Debug", "Release")) {
    throw "Configuration must be 'Debug' or 'Release'."
}

$ScriptRoot = $PSScriptRoot
$proj = Join-Path $ScriptRoot "X360Joystic\X360Joystic\X360Joystic.csproj"

Write-Host "Building: $proj ($Configuration)"
dotnet build $proj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

$exe = Join-Path $ScriptRoot "X360Joystic\X360Joystic\bin\$Configuration\net9.0-windows\X360Joystic.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Executable not found: $exe"
}

Write-Host "Starting: $exe"
Start-Process -FilePath $exe
