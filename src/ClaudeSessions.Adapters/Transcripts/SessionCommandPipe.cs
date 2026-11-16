using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Adapters.Transcripts;

/// <summary>
/// The local channel through which Conversations sends its commands to ClaudeSessions: a named pipe of
/// the current user only. A request is one JSON line <c>{"command":…,"tabId":…,"windowId":…,"text":…}</c>,
/// with command <c>prompt</c>, <c>show</c>, <c>window</c> or <c>tab</c>; the answer is one line holding the outcome's
/// name. <see cref="Listen"/> runs in ClaudeSessions and hands each command to its own
/// <see cref="ISessionCommands"/>; the methods of this class send them from Conversations.
/// </summary>
public sealed class SessionCommandPipe : ISessionCommands, IDisposable
{
    /// <summary>The pipe of the installed apps.</summary>
    public const string DefaultName = "ClaudeSessions.Commands";

    /// <summary>Longest request accepted, in characters; a longer one is dropped unanswered.</summary>
    public const int MaxRequestLength = 1_000_000;

    private const string PromptCommand = "prompt";
    private const string WindowCommand = "window";
    private const string TabCommand = "tab";
    private const string ShowCommand = "show";

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromSeconds(10);
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _name;
    private readonly CancellationTokenSource _stop = new();

    /// <summary>Creates the channel.</summary>
    /// <param name="name">The pipe's name; tests give their own.</param>
    public SessionCommandPipe(string name = DefaultName)
    {
        _name = name;
    }

    /// <summary>Answers commands until disposed, one at a time, on a background thread.</summary>
    /// <param name="commands">What carries the commands out.</param>
    public void Listen(ISessionCommands commands)
    {
        Task.Run(() => ListenAsync(commands));
    }

    /// <inheritdoc />
    public async Task<PromptOutcome> SendPromptAsync(TabId tabId, string text)
    {
        return Parse(await SendAsync(new CommandRequest(PromptCommand, tabId.ToString(), null, text)).ConfigureAwait(false), PromptOutcome.Unreachable);
    }

    /// <inheritdoc />
    public async Task<WindowOutcome> ShowTabAsync(TabId tabId)
    {
        return Parse(await SendAsync(new CommandRequest(ShowCommand, tabId.ToString(), null, null)).ConfigureAwait(false), WindowOutcome.Unreachable);
    }

    /// <inheritdoc />
    public async Task<WindowOutcome> OpenWindowAsync()
    {
        return Parse(await SendAsync(new CommandRequest(WindowCommand, null, null, null)).ConfigureAwait(false), WindowOutcome.Unreachable);
    }

    /// <inheritdoc />
    public async Task<WindowOutcome> OpenTabAsync(WindowId windowId)
    {
        return Parse(await SendAsync(new CommandRequest(TabCommand, null, windowId.ToString(), null)).ConfigureAwait(false), WindowOutcome.Unreachable);
    }

    /// <summary>Stops listening.</summary>
    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }

    private static TOutcome Parse<TOutcome>(string? answer, TOutcome unreachable)
        where TOutcome : struct, Enum
    {
        return Enum.TryParse(answer, out TOutcome outcome) && Enum.IsDefined(outcome) ? outcome : unreachable;
    }

    // Sends one request and returns the answer line; null when ClaudeSessions cannot be reached.
    private async Task<string?> SendAsync(CommandRequest request)
    {
        try
        {
            await using NamedPipeClientStream client = new(".", _name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using CancellationTokenSource timeout = new(ConnectTimeout + AnswerTimeout);
            await client.ConnectAsync(ConnectTimeout, timeout.Token).ConfigureAwait(false);
            using StreamWriter writer = new(client, Utf8, leaveOpen: true) { AutoFlush = true };
            using StreamReader reader = new(client, Utf8, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token).ConfigureAwait(false);
            return await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task ListenAsync(ISessionCommands commands)
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream server = new(_name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                await AnswerAsync(server, commands).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or JsonException or ObjectDisposedException)
            {
            }
        }
    }

    private async Task AnswerAsync(NamedPipeServerStream server, ISessionCommands commands)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        timeout.CancelAfter(AnswerTimeout);
        using StreamReader reader = new(server, Utf8, leaveOpen: true);
        using StreamWriter writer = new(server, Utf8, leaveOpen: true) { AutoFlush = true };
        string? line = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false);
        if (line is null || line.Length > MaxRequestLength || JsonSerializer.Deserialize<CommandRequest>(line) is not CommandRequest request)
        {
            return;
        }
        string? answer = await CarryOutAsync(request, commands).ConfigureAwait(false);
        if (answer is not null)
        {
            await writer.WriteLineAsync(answer.AsMemory(), timeout.Token).ConfigureAwait(false);
        }
    }

    // Runs a valid request and returns its outcome's name; null for an invalid request, left unanswered.
    private static async Task<string?> CarryOutAsync(CommandRequest request, ISessionCommands commands)
    {
        switch (request.Command)
        {
            case PromptCommand when TabId.TryParse(request.TabId, out TabId tabId) && !string.IsNullOrWhiteSpace(request.Text):
                return (await commands.SendPromptAsync(tabId, request.Text).ConfigureAwait(false)).ToString();
            case ShowCommand when TabId.TryParse(request.TabId, out TabId shown):
                return (await commands.ShowTabAsync(shown).ConfigureAwait(false)).ToString();
            case WindowCommand:
                return (await commands.OpenWindowAsync().ConfigureAwait(false)).ToString();
            case TabCommand when WindowId.TryParse(request.WindowId, out WindowId windowId):
                return (await commands.OpenTabAsync(windowId).ConfigureAwait(false)).ToString();
            default:
                return null;
        }
    }

    private sealed record CommandRequest(string Command, string? TabId, string? WindowId, string? Text);
}
