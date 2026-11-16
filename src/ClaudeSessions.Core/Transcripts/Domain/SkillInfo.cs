namespace ClaudeSessions.Core.Transcripts.Domain;

/// <summary>A skill a session can run as a slash command, as Claude Code listed it to the model.</summary>
/// <param name="Name">The skill's name, without the slash, such as ecole-publish or dumbometer:setup.</param>
/// <param name="Description">What it does, as its author wrote it.</param>
public sealed record SkillInfo(string Name, string Description);
