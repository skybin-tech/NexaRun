# Publishes all three NexaRun projects as self-contained win-x64 single-file binaries
# then compiles the Inno Setup installer.
#
# Usage (from repo root or installer/):
#   .\installer\build.ps1
#
# Requirements:
#   - .NET 10 SDK
#   - Inno Setup 6 installed (iscc.exe on PATH, or at default location)

param(
    [string]$Version = "1.0.0",
    [string]$OutDir  = "$PSScriptRoot\output"
)

$ErrorActionPreference = "Stop"
$Root    = Resolve-Path "$PSScriptRoot\.."
$PubDir  = "$PSScriptRoot\publish"

Write-Host "==> Building NexaRun $Version" -ForegroundColor Cyan

# Clean previous publish
if (Test-Path $PubDir) { Remove-Item $PubDir -Recurse -Force }
New-Item $PubDir -ItemType Directory | Out-Null

$PublishArgs = @(
    "--configuration", "Release",
    "--runtime",       "win-x64",
    "--self-contained", "true",
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:DebugType=none"
)

# Publish Daemon
Write-Host "==> Publishing Daemon..." -ForegroundColor Yellow
dotnet publish "$Root\NexaRun.Daemon" @PublishArgs --output "$PubDir\daemon"
if ($LASTEXITCODE -ne 0) { throw "Daemon publish failed" }

# Publish CLI
Write-Host "==> Publishing CLI..." -ForegroundColor Yellow
dotnet publish "$Root\NexaRun.Cli" @PublishArgs --output "$PubDir\cli"
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed" }

# Publish Tray
Write-Host "==> Publishing Tray..." -ForegroundColor Yellow
dotnet publish "$Root\NexaRun.Tray" @PublishArgs --output "$PubDir\tray"
if ($LASTEXITCODE -ne 0) { throw "Tray publish failed" }

# Compile Inno Setup installer
$ISCC = "iscc.exe"
if (-not (Get-Command $ISCC -ErrorAction SilentlyContinue)) {
    $ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $ISCC)) {
    throw "ISCC.exe not found. Install Inno Setup 6 or add it to PATH."
}

if (-not (Test-Path $OutDir)) { New-Item $OutDir -ItemType Directory | Out-Null }

Write-Host "==> Compiling installer..." -ForegroundColor Yellow
& $ISCC `
    "/DAppVersion=$Version" `
    "/DPubDir=$PubDir" `
    "/DOutDir=$OutDir" `
    "$PSScriptRoot\NexaRun.iss"

if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

Write-Host ""
Write-Host "==> Done! Installer at: $OutDir\NexaRun-$Version-Setup.exe" -ForegroundColor Green
