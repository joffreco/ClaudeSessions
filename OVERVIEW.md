# ClaudeSessions overview
<!-- overview · 2026-10-31 · HEAD 8a668b19fdbf -->

## Purpose
- App: Windows desktop app holding WSL Claude Code sessions in several windows with several tabs, one session per tab (README.md)
- Persistence: closed windows reopen with their tabs; after a reboot windows, tabs and sessions come back on their own (README.md)
- Resume: a tab resumes its session when a transcript ~/.claude/projects/*/<session>.jsonl exists, else starts a new one under the same id (README.md)
- Session tracking: /clear, /resume and forks are followed through a SessionStart hook writing one record per tab (README.md, wsl/claude-sessions-hook.sh)

## Stack
- Language: C# latest, .NET 10; Core and Adapters target net10.0, Windows app targets net10.0-windows WPF (src/*/*.csproj)
- Terminal: Windows Terminal WPF control from the unofficial CI.Microsoft.Terminal.Wpf package, connected through ConPTY (README.md, restore-terminal-control.sh)
- Packages: Microsoft.Windows.Console.ConPTY 1.25.260930003 (src/ClaudeSessions.Windows/ClaudeSessions.Windows.csproj)
- Build rules: nullable enabled, warnings as errors, code style enforced, lock files required (Directory.Build.props)
- Tests: xUnit 2.9.2, Microsoft.Extensions.TimeProvider.Testing (tests/ClaudeSessions.Tests/ClaudeSessions.Tests.csproj)

## Structure
- src/ClaudeSessions.Core: domain (Workspace, SessionWindow, SessionTab, ClosedTab, ids) and services (workspace, sync, zoom, wheel scroll) (src/ClaudeSessions.Core)
- src/ClaudeSessions.Adapters: JSON stores, settings file, hook record reader, OSC title parser, WSL command line, DTOs (src/ClaudeSessions.Adapters)
- src/ClaudeSessions.Windows: WPF hosting, keyboard router, single-instance gate, ConPTY terminal, views (src/ClaudeSessions.Windows)
- tests/ClaudeSessions.Tests: unit tests for Core and Adapters with in-memory fakes (tests/ClaudeSessions.Tests)
- wsl/: per-tab launcher, SessionStart hook and one-command setup script (wsl/claude-sessions-tab, wsl/claude-sessions-hook.sh, wsl/setup.sh)
- icon/: Python script drawing the .ico from pixel grids (icon/make_icon.py)

## Components
- Tab strip: acts as title bar with snap layouts, ☰ menu, equal-width tabs that scroll with arrows or wheel (README.md, src/ClaudeSessions.Windows/Views/TabRow.cs)
- Title marks: shows the ✳ or spinner state mark Claude Code puts in its title (README.md, src/ClaudeSessions.Adapters/OscTitleParser.cs)
- Failed session: tab kept with a banner offering resume, new session here, or close (README.md)
- Closed tabs: the 10 last closed tabs are kept; Ctrl+Shift+R reopens the last one (README.md)
- Shortcuts: new tab, new tab in chosen folder, close, reopen, new window, next/previous tab, copy/paste, zoom (README.md, src/ClaudeSessions.Windows/Hosting/KeyboardRouter.cs)
- Sync: hook records read every 30 s and on window close or quit, waiting at most 2 s (README.md, src/ClaudeSessions.Core/Application/SessionSynchronizer.cs)

## Data
- workspace.json: windows, tabs and closed tabs in %LOCALAPPDATA%\ClaudeSessions; unreadable file set aside as workspace.json.bad (README.md)
- settings.json: WSL executable, distro, WSL home, launcher path, default cwd, font family and size (README.md, src/ClaudeSessions.Adapters/AppSettings.cs)
- preferences.json: zoom and lines per wheel notch, absent when neither is set (README.md)
- Hook records: ~/.claude/session-tabs/<tab_id>.json, one per started tab (README.md)

## Integrations
- WSL: tabs run wsl.exe with the launcher; empty distro means the default distribution read from the registry (README.md, src/ClaudeSessions.Windows/Hosting/WslDefaultDistribution.cs)
- Claude Code: hook registered for SessionStart in ~/.claude/settings.json by wsl/setup.sh, previous file kept as .bak (README.md, wsl/setup.sh)

## Build and run
- Publish: ./publish.sh from WSL downloads the terminal control (pinned, SHA-512 checked) into vendor/terminal and publishes win-x64 into publish/ (README.md, publish.sh)
- Tests: dotnet test tests/ClaudeSessions.Tests; format check with dotnet format --verify-no-changes (README.md)
- Prerequisites: Windows 10 1809+ x64, Desktop Runtime 10; in WSL .NET 10 SDK, curl, openssl, unzip, jq, claude on PATH (README.md)

## Deploy
- Install: install.ps1 copies publish/ to %LOCALAPPDATA%\Programs\ClaudeSessions and adds Start menu and startup shortcuts; -NoStartup skips the latter (README.md, install.ps1)

## Related repos
- claude-tabs, hottabs: other tools of the owner around Claude Code sessions and tabs; not referenced in this repo (map.md of joff-ecosystem)

## Unknowns
- Tracked bytecode: icon/__pycache__/make_icon.cpython-313.pyc is committed; whether intended was not established (git ls-files)
- CI: no workflow file found (git ls-files)
