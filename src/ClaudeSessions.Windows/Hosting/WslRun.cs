namespace ClaudeSessions.Windows.Hosting;

/// <summary>Outcome of a step run in WSL.</summary>
/// <param name="Succeeded">True when the step exited with code 0.</param>
/// <param name="StandardOutput">Everything written on standard output.</param>
/// <param name="Tail">The last lines written on either stream, to explain a failure.</param>
internal sealed record WslRun(bool Succeeded, string StandardOutput, string Tail);
