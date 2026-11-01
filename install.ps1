<#
.SYNOPSIS
Installs claude-sessions for the current Windows user.

.DESCRIPTION
Copies the published app to %LOCALAPPDATA%\Programs\ClaudeSessions, then creates a Start menu
shortcut and, unless -NoStartup is given, a shortcut in shell:startup so the windows come back
at every logon. Run it from Windows PowerShell after ./publish.sh has run in WSL.

The previous version is kept in ClaudeSessions.previous and put back if the copy fails. When the
script runs from a WSL repository, it records the repository in install-source.json so that the
app can update itself from its menu (Mettre a jour).

.PARAMETER Source
Folder of the published app. Defaults to the publish folder next to this script.

.PARAMETER NoStartup
Do not start the app at logon.

.PARAMETER WaitForProcess
Id of the running app to wait for, at most 60 seconds, before copying. Used by the app when it
updates itself; the shortcuts are then left as they are and failures are shown in a message box.

.PARAMETER Restart
Start the app at the end, the new version or, after a failure, the previous one.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Ubuntu-24.04\home\joff\Projets\ClaudeSessions\install.ps1
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot 'publish'),
    [switch]$NoStartup,
    [int]$WaitForProcess = 0,
    [switch]$Restart
)

$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'Programs\ClaudeSessions'
$backup = "$target.previous"
$exe = Join-Path $target 'ClaudeSessions.exe'
$updating = $WaitForProcess -gt 0

function Wait-App {
    if ($updating) {
        $running = Get-Process -Id $WaitForProcess -ErrorAction SilentlyContinue
        if ($running -and -not $running.WaitForExit(60000)) {
            throw "ClaudeSessions (process $WaitForProcess) did not exit within 60 seconds."
        }
    }
    if (Get-Process -Name 'ClaudeSessions' -ErrorAction SilentlyContinue) {
        throw 'ClaudeSessions is running. Quit it from its menu (Quitter), then run this script again.'
    }
}

function Write-InstallSource {
    # \\wsl.localhost\<distro>\<path> or \\wsl$\<distro>\<path>; a Windows folder records nothing.
    if ($PSScriptRoot -match '^\\\\wsl(?:\.localhost|\$)\\([^\\]+)(\\.*)$') {
        $record = @{ distro = $Matches[1]; repoPath = ($Matches[2] -replace '\\', '/') }
        $record | ConvertTo-Json | Set-Content -Path (Join-Path $target 'install-source.json') -Encoding UTF8
    }
}

function Copy-App {
    if (Test-Path $backup) {
        Remove-Item -Path $backup -Recurse -Force
    }
    if (Test-Path $target) {
        Move-Item -Path $target -Destination $backup
    }
    try {
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        Copy-Item -Path (Join-Path $Source '*') -Destination $target -Recurse -Force
        Write-InstallSource
    }
    catch {
        if (Test-Path $target) {
            Remove-Item -Path $target -Recurse -Force
        }
        if (Test-Path $backup) {
            Move-Item -Path $backup -Destination $target
        }
        throw
    }
}

function Write-Shortcuts {
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
}

function Install-App {
    if (-not (Test-Path (Join-Path $Source 'ClaudeSessions.exe'))) {
        throw "ClaudeSessions.exe not found in $Source. Run ./publish.sh in WSL first."
    }
    $runtimes = & dotnet --list-runtimes 2>$null
    if (-not ($runtimes -match '^Microsoft\.WindowsDesktop\.App 10\.')) {
        Write-Warning 'Windows Desktop Runtime 10 is missing: winget install Microsoft.DotNet.DesktopRuntime.10'
    }
    Wait-App
    Copy-App
    if (-not $updating) {
        Write-Shortcuts
    }
}

$failure = $null
try {
    Install-App
}
catch {
    $failure = $_
}

if ($Restart -and (Test-Path $exe)) {
    Start-Process -FilePath $exe -WorkingDirectory $target
}

if ($failure) {
    if ($updating) {
        Add-Type -AssemblyName System.Windows.Forms
        $text = "The update failed; the previous version was kept.`n`n$failure"
        [System.Windows.Forms.MessageBox]::Show($text, 'Claude Sessions', 'OK', 'Error') | Out-Null
        exit 1
    }
    throw $failure
}

Write-Output "Installed to $target"
