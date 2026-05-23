# Ensures NexaRun\bin is on the Machine PATH and notifies Windows (run as Administrator).
# Used by the installer and can be run manually after upgrade if `nexarun` is not found.
param(
    [string]$InstallDir = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error 'Run this script as Administrator (right-click PowerShell → Run as administrator).'
    exit 1
}

$bin = Join-Path $InstallDir 'bin'
$exe = Join-Path $bin 'nexarun.exe'
if (-not (Test-Path $exe)) {
    Write-Error "nexarun.exe not found at: $exe"
    exit 1
}

$bin = (Resolve-Path $bin).Path.TrimEnd('\')
$machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
if ($null -eq $machinePath) { $machinePath = '' }

$alreadyOnPath = $false
foreach ($segment in $machinePath.Split(';', [StringSplitOptions]::RemoveEmptyEntries)) {
    if ($segment.Trim().TrimEnd('\').Equals($bin, [StringComparison]::OrdinalIgnoreCase)) {
        $alreadyOnPath = $true
        break
    }
}

if (-not $alreadyOnPath) {
    $newPath = if ([string]::IsNullOrWhiteSpace($machinePath)) { $bin } else { "$machinePath;$bin" }
    [Environment]::SetEnvironmentVariable('Path', $newPath, 'Machine')
    Write-Host "Added to system PATH: $bin"
}
else {
    Write-Host "Already on system PATH: $bin"
}

# Tell Explorer and other apps to reload environment variables
Add-Type @'
using System;
using System.Runtime.InteropServices;
internal static class EnvNotify {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
}
'@ | Out-Null

$dummy = [UIntPtr]::Zero
[void][EnvNotify]::SendMessageTimeout(
    [IntPtr]0xFFFF, 0x001A, [UIntPtr]::Zero, 'Environment', 0x0002, 5000, [ref]$dummy)

Write-Host ''
Write-Host 'Open a NEW Command Prompt or PowerShell window, then run:' -ForegroundColor Cyan
Write-Host '  nexarun version' -ForegroundColor White
Write-Host ''
exit 0
