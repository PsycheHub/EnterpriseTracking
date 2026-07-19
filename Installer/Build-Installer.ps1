# Build-Installer.ps1
# Builds the EnterpriseTrackingActivityAgent and packages it as a Windows installer.
# Run from the EnterpriseTracking\ solution root directory.
#
# Usage:
#   .\Installer\Build-Installer.ps1
#   .\Installer\Build-Installer.ps1 -Version "1.2.0"

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$SolutionDir = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $SolutionDir "EnterpriseTrackingActivityAgent\EnterpriseTrackingActivityAgent.csproj"
$PublishDir  = Join-Path $PSScriptRoot "publish"
$OutputDir   = Join-Path $PSScriptRoot "output"
$IssFile     = Join-Path $PSScriptRoot "setup.iss"
$IsccExe     = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host " Enterprise Tracking Activity Agent — Installer  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. Publish ────────────────────────────────────────────────────────────────
Write-Host "[1/2] Publishing self-contained exe (version $Version)..." -ForegroundColor Yellow

if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

dotnet publish $ProjectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "   Published to: $PublishDir" -ForegroundColor Green

# ── 2. Compile installer ──────────────────────────────────────────────────────
Write-Host "[2/2] Compiling Inno Setup installer..." -ForegroundColor Yellow

if (-not (Test-Path $IsccExe)) {
    throw "Inno Setup not found at: $IsccExe`nDownload from https://jrsoftware.org/isinfo.php"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

& $IsccExe "/DAppVersion=$Version" $IssFile

if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed." }

$InstallerPath = Join-Path $OutputDir "EnterpriseTrackingActivityAgent-Setup-$Version.exe"
$SizeMB = [math]::Round((Get-Item $InstallerPath).Length / 1MB, 1)

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host " Installer built successfully!" -ForegroundColor Green
Write-Host " Path : $InstallerPath" -ForegroundColor Green
Write-Host " Size : $SizeMB MB" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
