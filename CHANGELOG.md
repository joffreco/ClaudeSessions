# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Consignes app, merged into this repo as a second executable (`Consignes.exe`, project `src/ClaudeSessions.Consignes`): it shows the step-by-step instructions Claude sessions deposit, grouped by ClaudeSessions tab, with Markdown rendering, Copier buttons, tickable tasks, an unread count and an archive
- `consigne` command (`wsl/consigne`: `put`, `done`, `list`, `wait`) and `consignes` skill (`skill/consignes`) so Claude sessions can deposit instructions
- Session callback on tasks marked `<!-- callback -->`: a bell appears on the task, and `consigne wait` running in the background wakes the session when it is ticked
- Result fields in consignes: a `<!-- result: <name> -->` line shows a box where the user pastes a result, such as SQL rows, and sends it to the session, which `consigne wait` wakes with the pasted text
- Auto-sleep of idle tabs: ☰ « Veille des onglets inactifs » sets a delay of 1, 2, 4, 8 or 24 hours (off by default), after which a tab with no activity ends its process and frees its terminal; a panel with a « Réveiller » button relaunches it. The `wsl/claude-sessions-activity` probe is read before a tab sleeps, and `activityProbePath` in `settings.json` locates it
- `skill/breakpoint-consignes`: the breakpoint hand-test skill, shipped with the repo
- « Consignes (N non lues) » entry in the ClaudeSessions ☰ menu to launch Consignes
- Start menu and Startup shortcuts « Consignes »
- `consignesPollSeconds` setting in `settings.json`
- Consignes state stored in `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json`
- `wsl/setup.sh` links the `consigne` command and the skill, and creates `~/.claude/consignes`

### Changed

- The app follows the Windows light/dark mode live: the chrome is repainted, the ☰ menu and dialogs use the Fluent theme, and the terminals switch live too, Campbell in dark mode and One Half Light in light mode as in Windows Terminal, with the selected tab taking the terminal background
- `wsl/setup.sh` links every skill of `skill/` and the `claude-sessions-activity` probe
- `publish.sh` builds both executables, and the update from the ☰ menu updates both
- The update from the ☰ menu also installs uncommitted changes: a repository with uncommitted files is never reported up to date, and the question names how many files the update installs
- `install.ps1` closes and restarts Consignes, checks that every DLL loads (Smart App Control) and restores the previous version otherwise
- `AppPaths`, `SingleInstanceGate` and `WslDefaultDistribution` moved from `ClaudeSessions.Windows` to `ClaudeSessions.Adapters` so both executables share them

### Removed

- The standalone Consignes repo (`joffreco/Consignes`, never pushed) is superseded by this merge

### Fixed

- Window positions are restored in device pixels, so a window on a monitor with a different scaling than the primary one reopens at the right place and size
- A new tab takes the keyboard focus once its terminal exists
- The tab switcher fields are readable in light mode
