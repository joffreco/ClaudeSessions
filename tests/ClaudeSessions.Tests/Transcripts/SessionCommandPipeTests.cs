using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;
using Xunit;

namespace ClaudeSessions.Tests.Transcripts;

/// <summary>The channel through which Conversations sends its commands to ClaudeSessions.</summary>
public sealed class SessionCommandPipeTests
{
    private static readonly TabId Tab = new(Guid.Parse("11111111-2222-3333-4444-555555555555"));
    private static readonly WindowId Window = new(Guid.Parse("99999999-2222-3333-4444-555555555555"));

    /// <summary>A message reaches ClaudeSessions whole, and its outcome comes back.</summary>
    [Fact]
    public async Task Delivers_a_message_and_returns_its_outcome()
    {
        string name = PipeName();
        RecordingCommands received = new() { Prompt = PromptOutcome.AwaitingApproval };
        using SessionCommandPipe server = new(name);
        server.Listen(received);

        PromptOutcome outcome = await new SessionCommandPipe(name).SendPromptAsync(Tab, "Deux lignes\net « accents »");

        Assert.Equal(PromptOutcome.AwaitingApproval, outcome);
        Assert.Equal(["prompt " + Tab + " Deux lignes\net « accents »"], received.Calls);
    }

    /// <summary>New windows and tabs are asked for, one command after the other.</summary>
    [Fact]
    public async Task Opens_windows_and_tabs()
    {
        string name = PipeName();
        RecordingCommands received = new() { Opening = WindowOutcome.NoSuchWindow };
        using SessionCommandPipe server = new(name);
        server.Listen(received);
        SessionCommandPipe client = new(name);

        Assert.Equal(WindowOutcome.NoSuchWindow, await client.OpenWindowAsync());
        Assert.Equal(WindowOutcome.NoSuchWindow, await client.OpenTabAsync(Window));
        Assert.Equal(WindowOutcome.NoSuchWindow, await client.ShowTabAsync(Tab));
        Assert.Equal(["window", "tab " + Window, "show " + Tab], received.Calls);
    }

    /// <summary>Without ClaudeSessions listening, every command is unreachable, not an error.</summary>
    [Fact]
    public async Task Reports_unreachable_without_a_listener()
    {
        SessionCommandPipe client = new(PipeName());

        Assert.Equal(PromptOutcome.Unreachable, await client.SendPromptAsync(Tab, "personne"));
        Assert.Equal(WindowOutcome.Unreachable, await client.OpenWindowAsync());
    }

    private static string PipeName()
    {
        return "ClaudeSessions.Tests." + Guid.NewGuid().ToString("N");
    }

    private sealed class RecordingCommands : ISessionCommands
    {
        public PromptOutcome Prompt { get; init; } = PromptOutcome.Sent;

        public WindowOutcome Opening { get; init; } = WindowOutcome.Done;

        public List<string> Calls { get; } = [];

        public Task<PromptOutcome> SendPromptAsync(TabId tabId, string text)
        {
            Calls.Add($"prompt {tabId} {text}");
            return Task.FromResult(Prompt);
        }

        public Task<WindowOutcome> ShowTabAsync(TabId tabId)
        {
            Calls.Add($"show {tabId}");
            return Task.FromResult(Opening);
        }

        public Task<WindowOutcome> OpenWindowAsync()
        {
            Calls.Add("window");
            return Task.FromResult(Opening);
        }

        public Task<WindowOutcome> OpenTabAsync(WindowId windowId)
        {
            Calls.Add($"tab {windowId}");
            return Task.FromResult(Opening);
        }
    }
}
