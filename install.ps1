<#
.SYNOPSIS
Installs claude-sessions for the current Windows user.

.DESCRIPTION
Copies the published app to %LOCALAPPDATA%\Programs\ClaudeSessions, then creates a Start menu
shortcut and, unless -NoStartup is given, a shortcut in shell:startup so the windows come back
at every logon. Run it from Windows PowerShell after ./publish.sh has run in WSL.

.PARAMETER Source
Folder of the published app. Defaults to the publish folder next to this script.

.PARAMETER NoStartup
Do not start the app at logon.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Debian\home\joff\Sources\ClaudeSessions\install.ps1
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot 'publish'),
    [switch]$NoStartup
)

$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeSessions'
$exe = Join-Path $target 'ClaudeSessions.exe'

if (-not (Test-Path (Join-Path $Source 'ClaudeSessions.exe'))) {
    throw "ClaudeSessions.exe not found in $Source. Run ./publish.sh in WSL first."
}

$runtimes = & dotnet --list-runtimes 2>$null
if (-not ($runtimes -match '^Microsoft\.WindowsDesktop\.App 10\.')) {
    Write-Warning 'Windows Desktop Runtime 10 is missing: winget install Microsoft.DotNet.DesktopRuntime.10'
}

if (Get-Process -Name 'ClaudeSessions' -ErrorAction SilentlyContinue) {
    throw 'ClaudeSessions is running. Quit it from its menu (Quitter), then run this script again.'
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $Source '*') -Destination $target -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$shortcuts = @([Environment]::GetFolderPath('Programs'))
if (-not $NoStartup) {
    $shortcuts += [Environment]::GetFolderPath('Startup')
}
foreach ($folder in $shortcuts) {
    $link = $shell.CreateShortcut((Join-Path $folder 'Claude Sessions.lnk'))
    $link.TargetPath = $exe
    $link.WorkingDirectory = $target
    $link.Save()
}

Write-Output "Installed to $target"
