# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Conversations app (trial), a third executable (`Conversations.exe`, project `src/ClaudeSessions.Conversations`) with a Start menu shortcut, laid out as Consignes: a ☰ menu and the tabs of the open ClaudeSessions windows on the left, the selected tab's conversation on the right with the replies rendered in Markdown, the reasoning and the tool calls folded; it follows the active branch after `/rewind` and keeps the history before a compaction
- Writing to a session from Conversations: a field at the bottom (`Enter` sends, `Shift+Enter` new line, a draft per tab) hands the message to ClaudeSessions through the `ClaudeSessions.Commands` named pipe (`SessionCommandPipe`), and ClaudeSessions types it into the tab's terminal; nothing is sent while the session asks for an approval
- `src/ClaudeSessions.Ui`, a library holding the Markdown rendering of Consignes (`MarkdownDocumentBuilder`, `IMarkdownActions`, formerly `ConsigneDocumentBuilder` and `IConsigneActions`), shared with Conversations
- Command output and tool results lose the terminal's color codes in the conversation view
- New windows and tabs from Conversations (**Nouvelle fenêtre** button, `Ctrl+Shift+N`, `Ctrl+Shift+T`, right click on a window), carried out by ClaudeSessions; the new tab is selected once it shows up
- Status line in Conversations: `wsl/claude-sessions-statusline` wraps the user's `statusLine` command (installed by `wsl/setup.sh`), shows the same in Claude Code and keeps a copy per session in `~/.claude/session-status`, with its JSON input; Conversations shows it under the field in its colors, without the session name, each percentage as a pie and the main one of a segment larger
- Your last message in Conversations stays pinned at the top of the conversation once it scrolled out, three lines at most, the whole text in its tooltip; a click scrolls back to it
- Conversations has its own icon, two speech bubbles (a blue one with three dots in front of a coral one), drawn by `icon/make_conversations_icon.py`
- Conversations reads in a column of 780 pixels centered in the window, the field lined up under it, and draws your messages as rounded bubbles against its right edge, as wide as their text
- File changes of Edit and Write in Conversations, read from the transcript's `structuredPatch`: line numbers, removed lines in red, added lines in green
- The reasoning is shown in full in Conversations instead of folded
- Conversations shows a sent message at once until Claude Code records it, and reads the transcript every 250 ms for a few seconds after a send
- Activity in Conversations: while Claude works, a line with a spinner above the field gives the time since the prompt, the tokens written and the tool awaited; each turn ends with its duration or « Interrompu »; in the list, a spinner marks the tabs at work, a tab whose turn ended unseen turns green, and the taskbar button gets a green dot. Read from the transcript (`turn_duration`, `stop_reason`, `usage.output_tokens`, interruptions), every tab's end of file every second through `ITranscriptSource.ReadRecentAsync`
- Conversations sets the final response of each turn apart, with its time, and draws the progress updates between tool calls smaller and in secondary color
- Voice typing in Conversations: `Space` in the empty field turns the voice typing of Windows on, it stays on after a send, and `Space` turns it off
- Pasting images in Conversations (`Ctrl+V`, `Shift+Insert`, or dropping image files): saved in `~/.claude/session-images/`, shown as thumbnails above the field, sent as paths in the message and shown under it in the conversation
- Suggestions while typing `/` in Conversations: built-in commands and the session's skills, read from the transcript's `skill_listing`
- Slash and `!` commands are typed key by key into the terminal; a command that opens a menu (`/rewind`, `/resume`, `/config`, `/usage`…) brings its tab to the front in ClaudeSessions, as does the **Terminal** button (`Ctrl+Shift+G`)
- Conversations zooms its whole window (`Ctrl+=`, `Ctrl+-`, `Ctrl`+wheel, `Ctrl+0`), kept in `conversations.json`; the wheel scrolls the conversation over code blocks too, `Page Up` / `Page Down` scroll it a page at a time wherever the focus is, and a new message redraws only the entries that changed, so the scroll position and the unfolded blocks stay
- Read-only tab list for Conversations: `WorkspaceConversationTabSource` reads `workspace.json` without ever setting it aside, and `ConversationTabs` prefers the session the hook just recorded
- `publish.sh` builds Conversations, and `install.ps1` copies it, closes it before the copy and starts it again with `-Restart`
- Transcript driver: port `ITranscriptSource` (`Core/Transcripts`) and adapter `JsonlTranscriptSource` (`Adapters/Transcripts`), which reads `~/.claude/projects/<project>/<session>.jsonl` incrementally; the undocumented format is confined to the adapter, split into line syntax, branch chaining and message meaning
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
- The terminal of a tab (`TabTerminal`, `ConPtyConnection`, `PseudoConsoleProcess`, `TerminalThemes` and the ConPTY entry points) moved from `ClaudeSessions.Windows` to a library, `src/ClaudeSessions.Terminal`, so that Conversations can host sessions too; the app gives it the mode and the background brush
- `AppPaths`, `SingleInstanceGate` and `WslDefaultDistribution` moved from `ClaudeSessions.Windows` to `ClaudeSessions.Adapters` so both executables share them

### Removed

- The standalone Consignes repo (`joffreco/Consignes`, never pushed) is superseded by this merge

### Fixed

- Conversations kept following the bottom of the conversation only when the view sat there at the moment of a redraw: content that grew in between (an image decoded late, the activity line shown) made it stop, and new replies came in below the visible part. The view now follows the bottom until the user scrolls up, and goes back to it on a send

- Window positions are restored in device pixels, so a window on a monitor with a different scaling than the primary one reopens at the right place and size
- A new tab takes the keyboard focus once its terminal exists
- The tab switcher fields are readable in light mode
