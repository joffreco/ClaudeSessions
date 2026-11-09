using System;

namespace ClaudeSessions.Core.Domain;

/// <summary>What a tab's session did lately, as WSL sees it.</summary>
/// <param name="LastTranscriptWrite">Newest write to the transcript of the session or of its subagents; null when none exists.</param>
/// <param name="ChildProcesses">Live child processes of the tab's claude: background commands, monitors, running tools.</param>
public sealed record TabActivity(DateTimeOffset? LastTranscriptWrite, int ChildProcesses);
