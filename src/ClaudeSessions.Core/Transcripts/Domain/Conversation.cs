using System;
using System.Collections.Generic;
using ClaudeSessions.Core.Domain;

namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>The active branch of a Claude Code session, as the user saw it, oldest entry first.</summary>
/// <param name="Session">The session.</param>
/// <param name="Entries">What was said and done, in order.</param>
/// <param name="Skills">The skills the session can run, by name; empty until Claude Code lists them, at the first prompt.</param>
/// <param name="LastWrite">When Claude Code last wrote to the transcript; null when unknown.</param>
public sealed record Conversation(SessionId Session, IReadOnlyList<ConversationEntry> Entries, IReadOnlyList<SkillInfo> Skills, DateTimeOffset? LastWrite = null);
