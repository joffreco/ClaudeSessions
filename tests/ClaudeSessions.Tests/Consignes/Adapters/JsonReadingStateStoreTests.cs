using System;
using System.Collections.Generic;
using System.IO;
using ClaudeSessions.Adapters.Consignes;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Adapters;

/// <summary>Saving and loading what the user did with the consignes.</summary>
public sealed class JsonReadingStateStoreTests
{
    /// <summary>Seen versions and ticked tasks survive a save and a load.</summary>
    [Fact]
    public void RoundTrips()
    {
        using TempFolder folder = new();
        JsonReadingStateStore store = new(Path.Combine(folder.Path, "sub", "state.json"));
        DateTimeOffset seen = new(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-5));
        ReadingState state = new(new Dictionary<string, DateTimeOffset> { ["tab/one"] = seen }, []);
        state.SetTicked(new ConsigneKey("tab", "one"), 2, true);

        store.Save(state);
        ReadingState loaded = store.Load();

        Assert.Equal(seen, loaded.Seen["tab/one"]);
        Assert.True(loaded.IsTicked(new ConsigneKey("tab", "one"), 2));
        Assert.False(loaded.IsTicked(new ConsigneKey("tab", "one"), 1));
    }

    /// <summary>A missing or broken file loads as an empty state.</summary>
    [Fact]
    public void BrokenFileIsEmpty()
    {
        using TempFolder folder = new();
        string file = folder.Write("state.json", "{cassé");

        Assert.Empty(new JsonReadingStateStore(file).Load().Seen);
        Assert.Empty(new JsonReadingStateStore(Path.Combine(folder.Path, "absent.json")).Load().Seen);
    }
}
