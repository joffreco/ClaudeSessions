# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Consignes app, merged into this repo as a second executable (`Consignes.exe`, project `src/ClaudeSessions.Consignes`): it shows the step-by-step instructions Claude sessions deposit, grouped by ClaudeSessions tab, with Markdown rendering, Copier buttons, tickable tasks, an unread count and an archive
- `consigne` command (`wsl/consigne`: `put`, `done`, `list`, `wait`) and `consignes` skill (`skill/consignes`) so Claude sessions can deposit instructions
- Session callback on tasks marked `<!-- callback -->`: a bell appears on the task, and `consigne wait` running in the background wakes the session when it is ticked
- « Consignes (N non lues) » entry in the ClaudeSessions ☰ menu to launch Consignes
- Start menu and Startup shortcuts « Consignes »
- `consignesPollSeconds` setting in `settings.json`
- Consignes state stored in `%LOCALAPPDATA%\ClaudeSessions\consignes-state.json`
- `wsl/setup.sh` links the `consigne` command and the skill, and creates `~/.claude/consignes`

### Changed

- `publish.sh` builds both executables, and the update from the ☰ menu updates both
- The update from the ☰ menu also installs uncommitted changes: a repository with uncommitted files is never reported up to date, and the question names how many files the update installs
- `install.ps1` closes and restarts Consignes, checks that every DLL loads (Smart App Control) and restores the previous version otherwise
- `AppPaths`, `SingleInstanceGate` and `WslDefaultDistribution` moved from `ClaudeSessions.Windows` to `ClaudeSessions.Adapters` so both executables share them

### Removed

- The standalone Consignes repo (`joffreco/Consignes`, never pushed) is superseded by this merge
