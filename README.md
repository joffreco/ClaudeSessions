# ClaudeSessions

## Overview

`claude-sessions` is a Windows desktop app that holds the Claude Code sessions of WSL. It opens several windows, each with several tabs, and each tab runs one Claude Code session in a WSL directory. A closed window can be reopened with its tabs, which resume their sessions. After a reboot, the windows, their tabs and their sessions come back on their own.

As in Windows Terminal, the tab strip is the title bar: its empty part moves the window, a double-click maximizes it, and Windows 11 shows its snap layouts over the maximize button. The **☰** menu sits after the `+` button. The tabs share the width of the strip equally; once they no longer fit, they keep a narrow width and scroll with the **◀** **▶** arrows or the wheel, without a scroll bar, and the selected tab is brought into view.

The terminal is the Windows Terminal WPF control (same renderer as Windows Terminal), connected to the session through ConPTY. The app is C# / .NET 10 WPF; it is built from WSL and runs on Windows.

## Current Behavior Summary

| Action | Behavior |
|--------|----------|
| First start | Opens one window with one tab in the default directory. |
| Start | Reopens the windows that were open, at their place; a tab starts its session the first time it is shown. |
| Close a window, others open | Ends its sessions and lists it under **Fenêtres fermées** in the menu, from where it reopens with its tabs or is forgotten. |
| Close the last window | Quits; that window comes back at the next start. |
| **Quitter** (menu) | Saves and quits; every open window comes back at the next start. |
| Windows logoff or shutdown | Saves at once, without reading WSL; every open window comes back at the next logon. |
| `/exit` in a tab | Closes the tab and keeps it among the 10 last closed tabs. |
| Session fails (exit code other than 0) | Keeps the tab with a banner: **Reprendre**, **Nouvelle session ici**, **Fermer**. |
| `/clear`, `/resume`, fork in a tab | Followed: the tab resumes the new session next time. |

A tab resumes its session when a transcript of it exists (`~/.claude/projects/*/<session>.jsonl`), else it starts a new session under the same id. The tab title is the title the session sets, else the folder name. As in Windows Terminal, the tab shows the state mark Claude Code puts in front of its title: a green ✳ while the session waits for you, a spinner while it works. The mark is not saved, so a tab whose session has not started yet shows the title alone. The selected tab's title is in bold.

The app learns about `/clear`, `/resume` and forks from the `SessionStart` hook `wsl/claude-sessions-hook.sh`, which records the session of each tab in `~/.claude/session-tabs/<tab_id>.json`. The app reads these records every 30 seconds and when a window closes or the app quits, waiting at most 2 seconds; when a read fails, the tab keeps its last known session.

## Keyboard Shortcuts

| Keys | Action |
|------|--------|
| `Ctrl+Shift+T` | New tab in the directory of the current tab |
| `Ctrl+Shift+O` | New tab in a chosen directory |
| `Ctrl+Shift+W` or middle click | Close the tab (tabs have no close button, so a stray click never ends a running session) |
| `Ctrl+Shift+R` | Reopen the last closed tab |
| `Ctrl+Shift+N` | New window |
| Drag a tab | Move it within its window; the new order is saved at once |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Next / previous tab |
| `Ctrl+Shift+C` / `Ctrl+Shift+V` | Copy the selection / paste |
| `Ctrl+=` / `Ctrl+-` or `Ctrl`+wheel | Zoom in / out, one point at a time, in every window; the size is kept at the next start |
| `Ctrl+0` | Back to the `fontSize` of `settings.json` |
| `Enter` | Resume a failed session, while its banner is shown |

Every other key goes to the session.

## Prerequisites

- Windows 10 1809 or later, x64, with WSL and the distribution that runs Claude Code.
- Windows Desktop Runtime 10: `winget install Microsoft.DotNet.DesktopRuntime.10`.
- In WSL: the .NET 10 SDK, `curl`, `openssl`, `unzip`, `jq` (for the hook), and `claude` on the login shell's `PATH`.

## File Location

```text
src/, tests/                                     source (Core, Adapters, Windows) and tests
wsl/claude-sessions-tab                          launcher run in WSL for each tab
%LOCALAPPDATA%\Programs\ClaudeSessions\          installed app
%LOCALAPPDATA%\ClaudeSessions\workspace.json     windows, tabs and closed tabs
%LOCALAPPDATA%\ClaudeSessions\settings.json      settings
%LOCALAPPDATA%\ClaudeSessions\preferences.json   zoom and lines per wheel notch, absent when neither is set
~/.claude/session-tabs/<tab_id>.json             session of each tab, written by the hook
```

An unreadable `workspace.json` is set aside as `workspace.json.bad` and the app starts with a new window.

## Build

From WSL:

```bash
./publish.sh
```

`publish.sh` runs `restore-terminal-control.sh`, which downloads the `CI.Microsoft.Terminal.Wpf` package (pinned version, SHA-512 checked) into `vendor/terminal/`, then publishes the app for `win-x64` into `publish/`. The package is an unofficial repackaging of the Windows Terminal control on NuGet; its archive uses `\` separators that NuGet on Linux cannot read, hence the extraction by script. `unzip` warns about the separators while extracting; the warning is expected.

The icon, `src/ClaudeSessions.Windows/Assets/ClaudeSessions.ico`, is drawn by `icon/make_icon.py` from the pixel grids it holds; after changing them, regenerate it:

```bash
python3 icon/make_icon.py src/ClaudeSessions.Windows/Assets/ClaudeSessions.ico
```

Tests and checks:

```bash
dotnet test tests/ClaudeSessions.Tests
dotnet format ClaudeSessions.slnx --verify-no-changes
```

## Installation

1. Set up the WSL side:

   ```bash
   ./wsl/setup.sh
   ```

   It links the launcher into `~/.local/bin` and the hook into `~/.claude/hooks`, and registers the hook for `SessionStart` in `~/.claude/settings.json`, keeping the previous file as `settings.json.bak`. Running it again changes nothing.

2. From Windows PowerShell, run the installer:

   ```powershell
   powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Debian\home\joff\Sources\ClaudeSessions\install.ps1
   ```

   It copies `publish/` to `%LOCALAPPDATA%\Programs\ClaudeSessions` and creates a Start menu shortcut and a `shell:startup` shortcut. `-NoStartup` skips the second one; `-Source <folder>` installs from another folder. Quit the app before reinstalling.

If another tool also reopens Claude sessions at logon (such as `claude-restore` in Windows Terminal), keep only one of them at logon.

## Configuration

`settings.json` is written with its defaults at the first start:

| Key | Default | Meaning |
|-----|---------|---------|
| `wslExecutable` | `wsl.exe` | Executable started for each tab |
| `distro` | empty | WSL distribution; empty for the default distribution of WSL, read from the registry at each start |
| `wslHome` | `/home/joff` | Home of the WSL user; the hook records are under `<wslHome>/.claude/session-tabs` |
| `launcherPath` | `/home/joff/.local/bin/claude-sessions-tab` | WSL path of the launcher |
| `defaultCwd` | `/home/joff/Sources` | Directory of the first tab of a new window |
| `fontFamily` | `Cascadia Mono` | Terminal font |
| `fontSize` | `11` | Terminal font size, in points; the zoom starts from it and `Ctrl+0` comes back to it |

Restart the app after editing it.

The **☰** menu sets **Lignes par cran de molette**: 1, 3, 5 or 10 lines per wheel notch in every window, or the Windows setting. The choice is kept, with the zoom, in `preferences.json`; it sets how far the wheel scrolls the terminal history. As in Windows Terminal, the wheel scrolls the window under the mouse, even an inactive one, without a click first.

## Confirm It Works

Open two windows with two tabs each, run `/clear` in one tab, close one window, reopen it from **Fenêtres fermées**, quit with **Quitter**, start the app again, then reboot. Each time, the windows and tabs come back, and the tab where `/clear` ran resumes the new session.

```bash
ls ~/.claude/session-tabs/
```

shows one record per tab that has started.

## Troubleshooting

- **A tab shows « Session terminée »**: the launcher or `claude` failed. A deleted directory makes `wsl --cd` fail; choose **Nouvelle session ici** or close the tab.
- **Tabs never follow `/clear`**: the hook is missing or is an older version. Run `./wsl/setup.sh` again, restart the sessions, and check that `~/.claude/session-tabs/` fills up.
- **A `/clear` done just before a shutdown is lost**: the app does not read WSL at shutdown; the tab resumes the previous session.
- **Nothing happens on a second launch**: the running instance comes to the front instead; one instance runs per Windows session.
