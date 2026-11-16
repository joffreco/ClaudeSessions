using System.Threading.Tasks;
using System.Windows.Threading;
using ClaudeSessions.Core.Domain;
using ClaudeSessions.Core.Transcripts.Domain;
using ClaudeSessions.Core.Transcripts.Interfaces;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Carries out the commands Conversations sends, on the UI thread, through the coordinator.</summary>
/// <param name="dispatcher">The UI thread.</param>
/// <param name="coordinator">Drives the windows.</param>
internal sealed class CoordinatorCommands(Dispatcher dispatcher, WindowCoordinator coordinator) : ISessionCommands
{
    /// <inheritdoc />
    public Task<PromptOutcome> SendPromptAsync(TabId tabId, string text)
    {
        return dispatcher.InvokeAsync(() => coordinator.SubmitPrompt(tabId, text)).Task;
    }

    /// <inheritdoc />
    public Task<WindowOutcome> ShowTabAsync(TabId tabId)
    {
        return dispatcher.InvokeAsync(() => coordinator.ShowTab(tabId)).Task;
    }

    /// <inheritdoc />
    public Task<WindowOutcome> OpenWindowAsync()
    {
        return dispatcher.InvokeAsync(() =>
        {
            coordinator.NewWindow();
            return WindowOutcome.Done;
        }).Task;
    }

    /// <inheritdoc />
    public Task<WindowOutcome> OpenTabAsync(WindowId windowId)
    {
        return dispatcher.InvokeAsync(() => coordinator.NewTabIn(windowId)).Task;
    }
}
