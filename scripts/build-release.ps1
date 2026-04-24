param(
    [string]$Configuration = "Release",
    [string]$VersionSuffix = "",
    [switch]$SkipWindowsApp
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $repoRoot "artifacts"
$apiOutput = Join-Path $artifacts "api"
$windowsOutput = Join-Path $artifacts "windows"

if (Test-Path $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force
}

New-Item -ItemType Directory -Path $apiOutput -Force | Out-Null

$versionArg = @()
if (-not [string]::IsNullOrWhiteSpace($VersionSuffix)) {
    $versionArg = @("/p:VersionSuffix=$VersionSuffix")
}

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

Push-Location $repoRoot
try {
    Invoke-Dotnet restore Fillsquir.sln
    Invoke-Dotnet test tests/tests.csproj --no-restore --configuration Debug
    Invoke-Dotnet test FSquir.Api.Tests/FSquir.Api.Tests.csproj --no-restore --configuration Debug
    Invoke-Dotnet publish FSquir.Api/FSquir.Api.csproj --configuration $Configuration --output $apiOutput /p:UseAppHost=false @versionArg

    if (-not $SkipWindowsApp) {
        New-Item -ItemType Directory -Path $windowsOutput -Force | Out-Null
        Invoke-Dotnet publish Fillsquir/FSquir.csproj `
            --configuration $Configuration `
            --framework net10.0-windows10.0.19041.0 `
            --output $windowsOutput `
            /p:RuntimeIdentifier=win-x64 `
            /p:WindowsPackageType=None `
            @versionArg
    }
}
finally {
    Pop-Location
}

Write-Host "Release artifacts written to $artifacts"
