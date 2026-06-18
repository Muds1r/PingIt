# Builds a self-contained PingIt app and packages it as a Windows setup .exe.
# Run on Windows with .NET 8 SDK installed.
#
# Usage:
#   .\scripts\build-installer.ps1
#   .\scripts\build-installer.ps1 -Version 1.0.1
#   .\scripts\build-installer.ps1 -SkipInstaller   # publish only, no Inno Setup

param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "PingIt\PingIt.csproj"
$publishDir = Join-Path $repoRoot "dist\publish"
$installerDir = Join-Path $repoRoot "dist\installer"
$issFile = Join-Path $repoRoot "installer\PingIt.iss"

if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $csproj = Get-Content $project -Raw
    if ($csproj -match '<Version>([^<]+)</Version>') {
        $Version = $Matches[1]
    } else {
        $Version = "1.0.0"
    }
}

Write-Host "PingIt installer build"
Write-Host "  Version : $Version"
Write-Host "  Runtime : $Runtime"
Write-Host ""

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Publishing app (self-contained, no .NET install required on target PC)..."
dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Write-Host ""
Write-Host "Published to: $publishDir"

if ($SkipInstaller) {
    Write-Host "Skipping installer (-SkipInstaller)."
    exit 0
}

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host ""
    Write-Host "Inno Setup 6 not found." -ForegroundColor Yellow
    Write-Host "Install from: https://jrsoftware.org/isdl.php"
    Write-Host "Then run this script again, or compile manually:"
    Write-Host "  `"`$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe`" `"$issFile`" /DMyAppVersion=$Version /DPublishDir=$publishDir"
    exit 1
}

if (Test-Path $installerDir) {
    Remove-Item $installerDir -Recurse -Force
}
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

Write-Host ""
Write-Host "Building setup executable with Inno Setup..."
& $iscc $issFile "/DMyAppVersion=$Version" "/DPublishDir=$publishDir"

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compile failed."
}

$setupFile = Get-ChildItem $installerDir -Filter "PingIt-Setup-*.exe" | Select-Object -First 1
Write-Host ""
Write-Host "Done. Give users this file to install:" -ForegroundColor Green
Write-Host "  $($setupFile.FullName)"
