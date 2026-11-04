using ClaudeSessions.Core.Consignes.Application;
using ClaudeSessions.Core.Consignes.Domain;
using ClaudeSessions.Tests.Consignes.Fakes;
using Xunit;

namespace ClaudeSessions.Tests.Consignes.Core;

/// <summary>Counting the unread consignes for the ClaudeSessions menu.</summary>
public sealed class UnreadConsignesTests
{
    /// <summary>A consigne opened since its last version is read; a new one, or one updated after it was opened, is not.</summary>
    [Fact]
    public void CountsNewAndUpdatedConsignes()
    {
        Consigne seen = Make.Consigne("t", "vue");
        Consigne updated = Make.Consigne("t", "modifiee");
        ReadingState state = new();
        state.MarkSeen(seen);
        state.MarkSeen(updated);
        FakeConsigneSource source = new([seen, Make.Consigne("t", "modifiee", minute: 5), Make.Consigne("t", "nouvelle")]);

        Assert.Equal(2, UnreadConsignes.Count(source, new InMemoryReadingStateStore(state)));
    }
}
