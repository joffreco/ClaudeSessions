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
| **Vérifier les mises à jour** (menu) | Pulls the repository the app was installed from and compares commits; when they differ and you confirm, publishes, quits and comes back as the new version with every open window. |
| Remote desktop or console reconnection | Every session repaints its screen (Ctrl+L), the shown tab at once and the others when shown, since the reconnection leaves the terminals black. |
| Windows logoff or shutdown | Saves at once, without reading WSL; every open window comes back at the next logon. |
| `/exit` in a tab | Closes the tab and keeps it among the 10 last closed tabs. |
| Session fails (exit code other than 0) | Keeps the tab with a banner: **Reprendre**, **Nouvelle session ici**, **Fermer**. |
| `/clear`, `/resume`, fork in a tab | Followed: the tab resumes the new session next time. |
| A session finishes working while you do not watch it | Its tab turns green until you show it; when its window is not active, the window's taskbar button also gets a green dot until you click any window of the app. |
| A session asks for an approval while you do not watch it | Its tab turns orange until you show it; when its window is not active, the taskbar button gets an orange dot at once, until you click any window of the app. |

A tab resumes its session when a transcript of it exists (`~/.claude/projects/*/<session>.jsonl`), else it starts a new session under the same id. The tab title is the title the session sets, else the folder name. As in Windows Terminal, the tab shows the state mark Claude Code puts in front of its title: a green ✳ while the session waits for you, a spinner while it works. The mark is not saved, so a tab whose session has not started yet shows the title alone. The selected tab's title is in bold.

A session finishes working when its spinner gives way to the ✳. If that happens in a hidden tab, or in the shown tab of a window that is not active, the tab gets a green background until you show it in an active window; the window's taskbar button also gets a green dot when the window is not active and the session stayed idle for 1.5 seconds. A click anywhere in the app, even in a window already active, or the activation of one of its windows removes the dot of every window at once; so does the end of the wait of all its tabs. The green tabs stay green until you show them. Windows does not draw the dot when the taskbar uses small icons. A session that starts idle, as a resumed one does, has finished nothing and is not signalled. Orange takes precedence over green, on the tab and on the dot, and only showing the tab ends it, since only you can answer.

The app learns about `/clear`, `/resume` and forks from the `SessionStart` hook `wsl/claude-sessions-hook.sh`, which records the session of each tab in `%LOCALAPPDATA%\ClaudeSessions\session-tabs\<tab_id>.json`. The app gives the hook this folder through the `CLAUDE_SESSIONS_RECORDS` variable and `WSLENV`, watches it, and follows a change as soon as it happens, then saves; a `/clear` done just before a shutdown is therefore kept. It also reads the records every 30 seconds and when a window closes or the app quits, in case a notification was missed; when a read fails, the tab keeps its last known session. The same hook, on `PermissionRequest`, creates `<tab_id>.approval` in that folder, which the app reads and deletes; it decides nothing, so Claude Code asks you as usual.

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
%LOCALAPPDATA%\ClaudeSessions\session-tabs\      session of each tab and approval signals, written by the hook
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

   It links the launcher into `~/.local/bin` and the hook into `~/.claude/hooks`, and registers the hook for `SessionStart` and `PermissionRequest` in `~/.claude/settings.json`, keeping the previous file as `settings.json.bak`. Running it again changes nothing.

2. From Windows PowerShell, run the installer:

   ```powershell
   powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Ubuntu-24.04\home\joff\Projets\ClaudeSessions\install.ps1
   ```

   It copies `publish/` to `%LOCALAPPDATA%\Programs\ClaudeSessions` and creates a Start menu shortcut and a `shell:startup` shortcut. `-NoStartup` skips the second one; `-Source <folder>` installs from another folder. Quit the app before reinstalling. The previous version is kept in `ClaudeSessions.previous` and put back if the copy fails.

### Updating from the menu

Run from a WSL repository, the installer records the distribution and the repository path in `install-source.json` next to the app. **☰ → Vérifier les mises à jour** then does the rest while the sessions keep running:

1. In the repository, `git pull --ff-only` without any credentials prompt. A conflict, a diverged branch or uncommitted changes in the way stop here with the last lines of output, and the app keeps running.
2. The commit of the repository is compared with the one the running app was built from, which the .NET SDK stamps into its informational version (`1.0.0+<commit>`). When they match, the app says it is up to date.
3. Otherwise the app lists the subjects of the missing commits, names the sessions that are working (spinner in their tab) since their turn would be interrupted, and asks for confirmation.
4. Once confirmed, `./publish.sh` builds the new version; a failed build stops here and the app keeps running.
5. The app starts `install.ps1 -WaitForProcess <pid> -Restart` hidden, then quits as **Quitter** does. The installer waits for it to exit, copies the new version without touching the shortcuts, and starts the app, which reopens every window. If the copy fails, it puts the previous version back, starts it and shows the error.

An installation made before this feature, or from a Windows folder, has no `install-source.json`; run the installer once from the repository to enable the menu entry.

If another tool also reopens Claude sessions at logon (such as `claude-restore` in Windows Terminal), keep only one of them at logon.

## Configuration

`settings.json` is written with its defaults at the first start:

| Key | Default | Meaning |
|-----|---------|---------|
| `wslExecutable` | `wsl.exe` | Executable started for each tab |
| `distro` | empty | WSL distribution; empty for the default distribution of WSL, read from the registry at each start |
| `launcherPath` | `/home/joff/.local/bin/claude-sessions-tab` | WSL path of the launcher |
| `defaultCwd` | `/home/joff/Sources` | Directory of the first tab of a new window |
| `fontFamily` | `Cascadia Mono` | Terminal font |
| `fontSize` | `11` | Terminal font size, in points; the zoom starts from it and `Ctrl+0` comes back to it |

Restart the app after editing it.

The **☰** menu sets **Lignes par cran de molette**: 1, 3, 5 or 10 lines per wheel notch in every window, or the Windows setting. The choice is kept, with the zoom, in `preferences.json`; it sets how far the wheel scrolls the terminal history. As in Windows Terminal, the wheel scrolls the window under the mouse, even an inactive one, without a click first.

## Confirm It Works

Open two windows with two tabs each, run `/clear` in one tab, close one window, reopen it from **Fenêtres fermées**, quit with **Quitter**, start the app again, then reboot. Each time, the windows and tabs come back, and the tab where `/clear` ran resumes the new session.

```bash
ls "$(wslpath "$(cmd.exe /c echo %LOCALAPPDATA% 2>/dev/null | tr -d '\r')")/ClaudeSessions/session-tabs/"
```

shows one record per tab that has started.

## Troubleshooting

- **A tab shows « Session terminée »**: the launcher or `claude` failed. A deleted directory makes `wsl --cd` fail; choose **Nouvelle session ici** or close the tab.
- **Tabs never follow `/clear`**: the hook is missing or is an older version. Run `./wsl/setup.sh` again, restart the sessions, and check that `%LOCALAPPDATA%\ClaudeSessions\session-tabs\` fills up.
- **A tab never turns orange**: the hook is not registered for `PermissionRequest`. Run `./wsl/setup.sh` again; the tab must also have been started by this version of the app.
- **Nothing happens on a second launch**: the running instance comes to the front instead; one instance runs per Windows session.
