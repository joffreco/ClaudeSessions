using ClaudeSessions.Adapters;
using Xunit;

namespace ClaudeSessions.Tests.Adapters;

/// <summary>One running instance per application name.</summary>
public sealed class SingleInstanceGateTests
{
    /// <summary>The two applications of the suite do not share an instance lock.</summary>
    [Fact]
    public void Lets_two_different_applications_run_side_by_side()
    {
        string first = "GateTestA" + System.Guid.NewGuid().ToString("N");
        string second = "GateTestB" + System.Guid.NewGuid().ToString("N");

        using SingleInstanceGate? gateA = SingleInstanceGate.TryAcquire(first);
        using SingleInstanceGate? gateB = SingleInstanceGate.TryAcquire(second);

        Assert.NotNull(gateA);
        Assert.NotNull(gateB);
    }

    /// <summary>The application names the gates are built with are distinct.</summary>
    [Fact]
    public void Names_the_two_applications_differently()
    {
        Assert.NotEqual(SingleInstanceGate.ClaudeSessions, SingleInstanceGate.Consignes);
    }

    // category N/A: malicious — the name is a constant chosen by the application, no trust boundary crossed.
    // category N/A: second launch of the same name — the gate must be held on the acquiring thread and signals a pipe, not deterministic-safe to cover here.
}
