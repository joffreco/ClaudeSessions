# Deployment Notes

> Manual steps required to deploy each version of this project. Newest version on top.
> Maintained by the `version-notes` skill.

---

## Unreleased — 2026-11-04

No tag exists yet; this entry covers everything since the commit `33eb125`, up to the working tree on top of `origin/main` (hook records on the Windows side, approval signal, taskbar badge, quick tab switcher, AltGr, then Consignes, auto-sleep of idle tabs, window bounds in device pixels, light/dark mode).

### Breaking changes
- Consignes is merged into this repository as a second executable, `Consignes.exe`, installed in the same folder as `ClaudeSessions.exe` (`%LOCALAPPDATA%\Programs\ClaudeSessions`). The standalone Consignes install (`%LOCALAPPDATA%\Programs\Consignes`, its shortcuts and `%LOCALAPPDATA%\Consignes`) conflicts with it and must be removed; its `state.json` becomes `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json`.
- `publish.sh` and `install.ps1` now require both `ClaudeSessions.exe` and `Consignes.exe` (and `Markdig.dll`) in `publish/`; `install.ps1` fails when either exe is missing.
- `install.ps1` runs `Consignes.exe --check-install` after the copy; when Windows refuses to load an assembly (Smart App Control), it restores the previous version and exits with an error. Publishing again produces new file hashes.
- The update from the menu now also installs uncommitted changes: a repository with uncommitted files is never reported up to date, and the confirmation names how many files the update installs.
- The update from the menu adds the `Consignes` shortcuts next to existing `Claude Sessions` ones (Start menu, and Startup when that one is there); it never overwrites or recreates a shortcut removed by hand.
- The hook records and the approval signals are written in `%LOCALAPPDATA%\ClaudeSessions\session-tabs`, a folder the app passes to the hook through `WSLENV` (`CLAUDE_SESSIONS_RECORDS`). A tab started by an older app has no such variable: its session is still recorded in `~/.claude/session-tabs`, but its approvals are not signalled. Tabs relaunched by the updated app are not affected.
- `workspace.json` now stores the window bounds in device pixels and marks them with a `unit` field. A file without `unit` is read as the old device-independent bounds, which WPF applies once; the next save writes pixels. An older build reading a file saved by this one ignores `unit` and misplaces windows on a monitor whose scaling is not 100 %, so a rollback should be followed by moving them back.
- New setting `activityProbePath` in `settings.json` (default `/home/joff/.local/bin/claude-sessions-activity`): the WSL probe the app runs before it puts a tab to sleep. Without the link `setup.sh` creates, the probe fails and no tab ever sleeps; nothing else breaks. Auto-sleep is off until a delay is chosen in ☰ → « Veille des onglets inactifs » (1, 2, 4, 8 or 24 hours).

### Manual deployment steps
Run these in order:

1. **Link the commands, the hook and the skills (WSL)** — links `~/.local/bin/claude-sessions-activity` (new, the idle-sleep probe), `~/.local/bin/consigne`, and every skill of `skill/` into `~/.claude/skills` (`consignes` and the new `breakpoint-consignes`), creates `~/.claude/consignes`, and registers the hook for `SessionStart` and `PermissionRequest` in `~/.claude/settings.json` (previous file kept as `settings.json.bak`). Rerun it even where it ran before: without it the probe is not linked and tabs cannot sleep. Safe to run again. The update from the menu does not run it.
   ```bash
   ~/Sources/ClaudeSessions/wsl/setup.sh
   ```
2. **Remove the standalone Consignes install (Windows)** — delete `%LOCALAPPDATA%\Programs\Consignes`, its Start menu and Startup shortcuts, and `%LOCALAPPDATA%\Consignes`, after copying its `state.json` to `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json` so read and ticked states are kept. Already done on the development machine; any other machine that ran the standalone app needs it.
3. **Update from the menu, or quit and install by hand** — **☰ → Vérifier les mises à jour** does the whole update and adds the `Consignes` shortcuts next to the `Claude Sessions` ones. To install by hand instead, quit ClaudeSessions first (a running Consignes is closed by the script itself).
4. **By hand only (Windows PowerShell)** — copies both apps, checks that every DLL loads, creates the `Consignes` Start menu and Startup shortcuts next to the `Claude Sessions` ones, and starts ClaudeSessions again (and Consignes if it was running). `publish/` must hold the current build: run `./publish.sh` in WSL first if it does not.
   ```powershell
   powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Debian\home\joff\Sources\ClaudeSessions\install.ps1 -Restart
   ```
   Later updates can go through the ☰ menu, which also updates both executables (but not the WSL links of step 1: rerun `setup.sh` when a release adds a script or a skill).

### Notes
- Verify: `ls -l ~/.local/bin/claude-sessions-activity ~/.claude/skills/breakpoint-consignes` point into this repository, and `~/.local/bin/claude-sessions-activity <tab_uuid> <session_uuid>` prints `transcript=` and `children=` lines; the ☰ menu of ClaudeSessions shows « Veille des onglets inactifs » and « Consignes (N non lues) » (which starts `Consignes.exe`); in a Claude session, `consigne list` runs and `ls -l ~/.claude/skills/consignes` points into this repository.
- Rollback: `install.ps1` keeps the previous version in `ClaudeSessions.previous` and restores it on a failed copy or a refused assembly. The shortcuts and the removal of the standalone app are not undone by it.
- Light/dark mode needs no step: the terminals follow the Windows app mode live (Campbell in dark mode, One Half Light in light mode, the selected tab taking the terminal background), with no new setting, file, script or WSL link, and it ships through ☰ → Vérifier les mises à jour. Verify by switching the Windows app mode (Settings → Personalization → Colors): the tab strip and the open terminals repaint at once, and a sleeping tab takes the mode in force when it wakes. Terminals no longer stay dark in light mode; Claude Code's own theme is still chosen with `/theme` in a session.
- New optional setting `consignesPollSeconds` in `settings.json` (default 2); nothing to add.
