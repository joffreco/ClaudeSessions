# Deployment Notes

> Manual steps required to deploy each version of this project. Newest version on top.
> Maintained by the `version-notes` skill.

---

## Unreleased — 2026-11-04

No tag exists yet; this entry covers everything since the commit `33eb125`.

### Breaking changes
- Consignes is merged into this repository as a second executable, `Consignes.exe`, installed in the same folder as `ClaudeSessions.exe` (`%LOCALAPPDATA%\Programs\ClaudeSessions`). The standalone Consignes install (`%LOCALAPPDATA%\Programs\Consignes`, its shortcuts and `%LOCALAPPDATA%\Consignes`) conflicts with it and must be removed; its `state.json` becomes `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json`.
- `publish.sh` and `install.ps1` now require both `ClaudeSessions.exe` and `Consignes.exe` (and `Markdig.dll`) in `publish/`; `install.ps1` fails when either exe is missing.
- `install.ps1` runs `Consignes.exe --check-install` after the copy; when Windows refuses to load an assembly (Smart App Control), it restores the previous version and exits with an error. Publishing again produces new file hashes.
- The update from the menu now also installs uncommitted changes: a repository with uncommitted files is never reported up to date, and the confirmation names how many files the update installs.
- The update from the menu adds the `Consignes` shortcuts next to existing `Claude Sessions` ones (Start menu, and Startup when that one is there); it never overwrites or recreates a shortcut removed by hand.

### Manual deployment steps
Run these in order:

1. **Link the `consigne` command and the skill (WSL)** — links `~/.local/bin/consigne` and `~/.claude/skills/consignes` to this repository and creates `~/.claude/consignes`. Safe to run again.
   ```bash
   ~/Sources/ClaudeSessions/wsl/setup.sh
   ```
2. **Remove the standalone Consignes install (Windows)** — delete `%LOCALAPPDATA%\Programs\Consignes`, its Start menu and Startup shortcuts, and `%LOCALAPPDATA%\Consignes`, after copying its `state.json` to `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json` so read and ticked states are kept. Already done on the development machine; any other machine that ran the standalone app needs it.
3. **Update from the menu, or quit and install by hand** — **☰ → Vérifier les mises à jour** does the whole update and adds the `Consignes` shortcuts next to the `Claude Sessions` ones. To install by hand instead, quit ClaudeSessions first (a running Consignes is closed by the script itself).
4. **By hand only (Windows PowerShell)** — copies both apps, checks that every DLL loads, creates the `Consignes` Start menu and Startup shortcuts next to the `Claude Sessions` ones, and starts ClaudeSessions again (and Consignes if it was running). `publish/` must hold the current build: run `./publish.sh` in WSL first if it does not.
   ```powershell
   powershell -ExecutionPolicy Bypass -File \\wsl.localhost\Debian\home\joff\Sources\ClaudeSessions\install.ps1 -Restart
   ```
   Later updates can go through the ☰ menu, which also updates both executables.

### Notes
- Verify: the ☰ menu of ClaudeSessions shows « Consignes (N non lues) » and starts `Consignes.exe`; in a Claude session, `consigne list` runs and `ls -l ~/.claude/skills/consignes` points into this repository.
- Rollback: `install.ps1` keeps the previous version in `ClaudeSessions.previous` and restores it on a failed copy or a refused assembly. The shortcuts and the removal of the standalone app are not undone by it.
- New optional setting `consignesPollSeconds` in `settings.json` (default 2); nothing to add.
