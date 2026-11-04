# ClaudeSessions

## Overview

`claude-sessions` is a Windows desktop app that holds the Claude Code sessions of WSL. It opens several windows, each with several tabs, and each tab runs one Claude Code session in a WSL directory. A closed window can be reopened with its tabs, which resume their sessions. After a reboot, the windows, their tabs and their sessions come back on their own.

As in Windows Terminal, the tab strip is the title bar: its empty part moves the window, a double-click maximizes it, and Windows 11 shows its snap layouts over the maximize button. The **☰** menu sits after the `+` button. The tabs share the width of the strip equally; once they no longer fit, they keep a narrow width and scroll with the **◀** **▶** arrows or the wheel, without a scroll bar, and the selected tab is brought into view.

The terminal is the Windows Terminal WPF control (same renderer as Windows Terminal), connected to the session through ConPTY. The app is C# / .NET 10 WPF; it is built from WSL and runs on Windows.

A second executable, `Consignes.exe`, ships in the same folder: it shows the step-by-step instructions (consignes) that Claude Code sessions deposit for you, grouped by tab (see [Consignes](#consignes)). It has no installer or update of its own; both go through ClaudeSessions.

## Current Behavior Summary

| Action | Behavior |
|--------|----------|
| First start | Opens one window with one tab in the default directory. |
| Start | Reopens the windows that were open, at their place; a tab starts its session the first time it is shown. |
| Close a window, others open | Ends its sessions and lists it under **Fenêtres fermées** in the menu, from where it reopens with its tabs or is forgotten. |
| Close the last window | Quits; that window comes back at the next start. |
| **Quitter** (menu) | Saves and quits; every open window comes back at the next start. |
| **Vérifier les mises à jour** (menu) | Pulls the repository the apps were installed from and compares commits; when they differ and you confirm, publishes, quits and comes back as the new version with every open window. A running Consignes is closed and started again. |
| **Consignes (N non lues)** (menu) | Starts `Consignes.exe`, or brings the running one to the front; the entry shows the number of unread consignes when there are some. |
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
| `Ctrl+Shift+P` | Jump to a tab of any window: type to filter on the title or the folder, `↑` `↓` to move, `Enter` to go, `Esc` to cancel; tabs asking for an approval come first, then finished sessions |
| Drag a tab | Move it within its window; the new order is saved at once |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Next / previous tab |
| `Ctrl+Shift+C` / `Ctrl+Shift+V` | Copy the selection / paste |
| `Ctrl+=` / `Ctrl+-` or `Ctrl`+wheel | Zoom in / out, one point at a time, in every window; the size is kept at the next start |
| `Ctrl+0` | Back to the `fontSize` of `settings.json` |
| `Enter` | Resume a failed session, while its banner is shown |

Every other key goes to the session.

## Consignes

`Consignes.exe` lists the procedures that Claude Code sessions deposit for you. A session writes a procedure as a Markdown file with the `consigne` command in WSL; the app lists it under the ClaudeSessions tab of that session, renders it with a **Copier** button on each code block and boxes to tick, and counts the unread ones in its title. A consigne you are done with is archived. The app reads `~/.claude/consignes` through `\\wsl.localhost\<distro>` and the ClaudeSessions `workspace.json` to name and place each session's consignes. Nothing is written back to the consignes except by the archive and by tick events (see Callback on a ticked step).

| Action | Behavior |
|--------|----------|
| Start | Reads the consignes and refreshes every `consignesPollSeconds` (2 by default); one instance runs per Windows session, a second launch brings the running window to the front. |
| Left tree | One node per ClaudeSessions tab, named after the tab title, else the title recorded by the session, else the folder of its `cwd`. The most recently updated group comes first. |
| Consigne outside a tab | Grouped by the folder of its `cwd`, labelled `<folder> (hors onglet)`. |
| Tab closed in ClaudeSessions | Its group moves under **Onglets fermés**. |
| Unread | A consigne is unread until opened, and again when its file is updated; it shows a ● and a bold title. The window title reads `Consignes (N)` for N unread consignes. |
| Status `en-revision` | Shown with an **en révision** badge and « ne pas exécuter » in the header, until the session deposits it again as `prete`. |
| Code block | Rendered with a **Copier** button that puts the block on the clipboard. |
| Task (`- [ ] …`) | Rendered as a box; ticks are kept per consigne in `consignes-state.json`. A box already `[x]` in the file shows ticked. |
| Task marked `<!-- callback -->` | Shows a 🔔; ticking it tells the session (see below). |
| **Archiver** | Moves the consigne to the `archive/` folder of its session, with its event files. |
| **☰ → Ouvrir le dossier des consignes** | Opens `~/.claude/consignes` in Explorer. |
| **☰ → Quitter** | Quits. |

A consigne that was deposited again under the same id replaces the previous version in place and comes back as unread. Markdown is rendered for what a session writes: headings, paragraphs, lists, task lists, quotes, rules, pipe tables, fenced code and inline styles.

`<key>` in `~/.claude/consignes/<key>/<id>.md` is the `tab_id` of the ClaudeSessions tab, which stays the same through `/clear` and `/resume`. Outside a tab it is `_sans-onglet/<folder of the cwd>`. A consigne has a front matter (`title`, `status`, `tab_id`, `tab_title`, `session_id`, `cwd`, `created`, `updated`) followed by the body. Archived ones are in `~/.claude/consignes/<key>/archive/`.

### Depositing a consigne

```bash
consigne put --title "<title>" [--id <id>] [--status en-revision|prete] <<'CONSIGNE'
<Markdown>
CONSIGNE
consigne list
consigne done <id>
consigne wait <id|file.md> [--timeout <seconds>]
```

- `put` reads the Markdown on stdin and writes `<id>.md` atomically. Without `--id`, the id is a slug of the title followed by the time. An id matches `[a-z0-9][a-z0-9-]{0,63}`. The status defaults to `prete`. Depositing again with the same id keeps `created` and updates `updated`. It prints `id:`, `fichier:` and `callbacks:` (the number of marked tasks). A missing title, a bad id or status, or an empty body fails with code 2.
- `list` prints the id, status and title of the consignes of the current session.
- `done` archives a consigne with its event files.
- `wait` blocks until a marked step is ticked (see below).
- The tab of a session is known from `CLAUDE_SESSIONS_TAB` and `CLAUDE_SESSIONS_TAB_PID`, set by ClaudeSessions; a nested or background `claude` does not own the tab and files its consignes by folder.

The skill `skill/consignes/SKILL.md`, linked by `wsl/setup.sh`, tells sessions to deposit every procedure they hand you, in addition to showing it in the answer.

### Callback on a ticked step

A task ending with `<!-- callback -->` wakes the session when you tick it:

```markdown
- [ ] Lancer la migration <!-- callback -->
```

The app appends a line `{"index":…,"text":…,"at":…}` to `<id>.events.jsonl`. The session runs `consigne wait <file>` in the background; it prints `Étape N cochée : <text>` for each line it has not seen and exits. Unticking sends nothing. If the line cannot be written, the app shows an error and leaves the box unticked.

| Exit code | Meaning |
|-----------|---------|
| 0 | One or more marked steps were ticked. |
| 1 | No consigne at that path or id when `wait` starts. |
| 2 | Bad usage, id or file. |
| 3 | The consigne was archived. |
| 4 | No tick within the timeout (3600 s by default). |

Ticks made while no `wait` runs are not lost: the next `wait` gives them at once, tracked in `<id>.events.seen`.

## Prerequisites

- Windows 10 1809 or later, x64, with WSL and the distribution that runs Claude Code.
- Windows Desktop Runtime 10: `winget install Microsoft.DotNet.DesktopRuntime.10`.
- In WSL: the .NET 10 SDK, `curl`, `openssl`, `unzip`, `jq` (for the hook and `consigne`), `flock` and `iconv` (for `consigne`), and `claude` on the login shell's `PATH`.

## File Location

```text
src/, tests/                                     source (Core, Adapters, Windows, Consignes) and tests
src/ClaudeSessions.Consignes/                    the Consignes app (WPF window and hosting)
src/ClaudeSessions.Core/Consignes/               consignes domain and application logic, no I/O
src/ClaudeSessions.Adapters/Consignes/           consigne files, Markdown, reading state
wsl/claude-sessions-tab                          launcher run in WSL for each tab
wsl/consigne                                     the command a session runs to deposit a consigne
wsl/tests/consigne-test.sh                       tests of wsl/consigne
skill/consignes/SKILL.md                         Claude skill that tells sessions to deposit
icon/make_consignes_icon.py                      draws the Consignes icon
%LOCALAPPDATA%\Programs\ClaudeSessions\          installed apps (ClaudeSessions.exe and Consignes.exe)
%LOCALAPPDATA%\ClaudeSessions\workspace.json     windows, tabs and closed tabs
%LOCALAPPDATA%\ClaudeSessions\settings.json      settings
%LOCALAPPDATA%\ClaudeSessions\preferences.json   zoom and lines per wheel notch, absent when neither is set
%LOCALAPPDATA%\ClaudeSessions\session-tabs\      session of each tab and approval signals, written by the hook
%LOCALAPPDATA%\ClaudeSessions\consignes-state.json   what was read and ticked in Consignes
~/.claude/consignes/<key>/<id>.md                a consigne
```

An unreadable `workspace.json` is set aside as `workspace.json.bad` and the app starts with a new window.

## Build

From WSL:

```bash
./publish.sh
```

`publish.sh` runs `restore-terminal-control.sh`, which downloads the `CI.Microsoft.Terminal.Wpf` package (pinned version, SHA-512 checked) into `vendor/terminal/`, then publishes both apps, ClaudeSessions and Consignes, for `win-x64` into `publish/`. The package is an unofficial repackaging of the Windows Terminal control on NuGet; its archive uses `\` separators that NuGet on Linux cannot read, hence the extraction by script. `unzip` warns about the separators while extracting; the warning is expected.

The icon, `src/ClaudeSessions.Windows/Assets/ClaudeSessions.ico`, is drawn by `icon/make_icon.py` from the pixel grids it holds; after changing them, regenerate it:

```bash
python3 icon/make_icon.py src/ClaudeSessions.Windows/Assets/ClaudeSessions.ico
```

The Consignes icon, `src/ClaudeSessions.Consignes/Assets/Consignes.ico`, is a bell drawn by `icon/make_consignes_icon.py`; regenerate it the same way:

```bash
python3 icon/make_consignes_icon.py src/ClaudeSessions.Consignes/Assets/Consignes.ico
```

Tests and checks:

```bash
dotnet test tests/ClaudeSessions.Tests
wsl/tests/consigne-test.sh
dotnet format ClaudeSessions.slnx --verify-no-changes
```

## Installation

1. Set up the WSL side:

   ```bash
   ./wsl/setup.sh
   ```

   It links the launcher and the `consigne` command into `~/.local/bin`, the hook into `~/.claude/hooks` and the skill into `~/.claude/skills/consignes`, creates `~/.claude/consignes`, and registers the hook for `SessionStart` and `PermissionRequest` in `~/.claude/settings.json`, keeping the previous file as `settings.json.bak`. Running it again changes nothing.

2. From Windows PowerShell, run the installer:

   ```powershell
   powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Debian\home\joff\Sources\ClaudeSessions\install.ps1
   ```

   It copies `publish/` to `%LOCALAPPDATA%\Programs\ClaudeSessions`, checks that Windows lets every assembly load (`Consignes.exe --check-install`; Smart App Control blocks some unsigned builds), and creates a `Claude Sessions` and a `Consignes` shortcut in the Start menu and in `shell:startup`. `-NoStartup` skips the startup shortcuts; `-Source <folder>` installs from another folder; `-Restart` starts the app at the end, and Consignes too when it was running. A running Consignes is closed first; quit ClaudeSessions before reinstalling. The previous version is kept in `ClaudeSessions.previous` and put back if the copy fails or Windows refuses an assembly.

### Updating from the menu

Run from a WSL repository, the installer records the distribution and the repository path in `install-source.json` next to the app. **☰ → Vérifier les mises à jour** then does the rest while the sessions keep running:

1. In the repository, `git pull --ff-only` without any credentials prompt. A conflict, a diverged branch or uncommitted changes in the way stop here with the last lines of output, and the app keeps running.
2. The commit of the repository is compared with the one the running app was built from, which the .NET SDK stamps into its informational version (`1.0.0+<commit>`). When they match and the repository has no uncommitted file (`git status --porcelain`), the app says it is up to date; a repository with uncommitted files is never reported up to date.
3. Otherwise the app lists the subjects of the missing commits, names the sessions that are working (spinner in their tab) since their turn would be interrupted, and asks for confirmation. When the repository has uncommitted files, the question says how many and that the update installs them too.
4. Once confirmed, `./publish.sh` builds the new version; a failed build stops here and the app keeps running.
5. The app starts `install.ps1 -WaitForProcess <pid> -Restart` hidden, then quits as **Quitter** does. The installer waits for it to exit, closes Consignes if it runs, copies the new version, adds a missing `Consignes` shortcut only where a `Claude Sessions` shortcut exists (existing shortcuts are left as they are), and starts the app, which reopens every window, then Consignes if it was running. If the copy fails, it puts the previous version back, starts it and shows the error.

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
| `consignesPollSeconds` | `2` | Seconds between two reads of the consignes by Consignes (at least 1) |

Restart the app, and Consignes, after editing it.

Environment variables of `consigne`:

| Variable | Default | Meaning |
|----------|---------|---------|
| `CONSIGNES_ROOT` | `~/.claude/consignes` | Where consignes are written |
| `CLAUDE_SESSION_TABS` | `~/.claude/session-tabs` | Records of each tab, for the `session_id` |
| `CONSIGNES_WORKSPACE` | `workspace.json`, found through `%LOCALAPPDATA%` | Source of the tab title |

The **☰** menu sets **Lignes par cran de molette**: 1, 3, 5 or 10 lines per wheel notch in every window, or the Windows setting. The choice is kept, with the zoom, in `preferences.json`; it sets how far the wheel scrolls the terminal history. As in Windows Terminal, the wheel scrolls the window under the mouse, even an inactive one, without a click first.

## Confirm It Works

Open two windows with two tabs each, run `/clear` in one tab, close one window, reopen it from **Fenêtres fermées**, quit with **Quitter**, start the app again, then reboot. Each time, the windows and tabs come back, and the tab where `/clear` ran resumes the new session.

```bash
ls "$(wslpath "$(cmd.exe /c echo %LOCALAPPDATA% 2>/dev/null | tr -d '\r')")/ClaudeSessions/session-tabs/"
```

shows one record per tab that has started.

To check Consignes, deposit a test consigne from a session tab, then archive it:

```bash
echo '- [ ] Test <!-- callback -->' | consigne put --title "Essai" --id essai
consigne wait essai &
```

It appears in Consignes under the tab of the session, unread; ticking the box makes `wait` print `Étape 1 cochée : Test` and exit.

```bash
consigne done essai
```

## Troubleshooting

- **A tab shows « Session terminée »**: the launcher or `claude` failed. A deleted directory makes `wsl --cd` fail; choose **Nouvelle session ici** or close the tab.
- **Tabs never follow `/clear`**: the hook is missing or is an older version. Run `./wsl/setup.sh` again, restart the sessions, and check that `%LOCALAPPDATA%\ClaudeSessions\session-tabs\` fills up.
- **A tab never turns orange**: the hook is not registered for `PermissionRequest`. Run `./wsl/setup.sh` again; the tab must also have been started by this version of the app.
- **Nothing happens on a second launch**: the running instance comes to the front instead; one instance runs per Windows session.
- **« Aucune consigne »**: nothing was deposited, or `distro` and `wslHome` do not point at the WSL home that holds `.claude/consignes`.
- **A Consignes group is named after a folder, with « (hors onglet) »**: the session ran `consigne` outside a ClaudeSessions tab, or from a nested `claude`.
- **A tick does not reach the session**: Consignes shows an error and leaves the box unticked when `<id>.events.jsonl` cannot be written; check that `consigne wait` is running for that file.
- **The installer says Windows refused an assembly**: publish again to get new file hashes, then reinstall.
