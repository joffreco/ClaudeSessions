using System;
using System.Windows;
using ClaudeSessions.Adapters;
using ClaudeSessions.Adapters.Transcripts;
using ClaudeSessions.Conversations.Views;
using ClaudeSessions.Core.Transcripts.Application;

namespace ClaudeSessions.Conversations.Hosting;

/// <summary>Entry point and composition root: wires the read-only adapters to the window.</summary>
internal sealed class App : Application
{
    private const string FluentTheme = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml";

    private readonly SingleInstanceGate _gate;

    private App(SingleInstanceGate gate)
    {
        _gate = gate;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    /// <summary>Starts the application, or brings the running instance to the front.</summary>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main()
    {
        SingleInstanceGate? gate = SingleInstanceGate.TryAcquire(SingleInstanceGate.Conversations);
        if (gate is null)
        {
            return 0;
        }
        using (gate)
        {
            App app = new(gate);
            return app.Run();
        }
    }

    /// <summary>Builds the object graph and opens the window.</summary>
    /// <param name="e">Startup arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(FluentTheme) });
        AppSettings settings = AppSettingsFile.Load(AppPaths.SettingsFile, WslDefaultDistribution.Read());
        ConversationTabs tabs = new(
            new WorkspaceConversationTabSource(AppPaths.WorkspaceFile),
            new HookFileTabSessionSource(AppPaths.SessionRecordsFolder));
        MainWindow window = new(
            tabs,
            new JsonlTranscriptSource(settings.ProjectsDirectory),
            new SessionCommandPipe(),
            new StatusLineFiles(settings.StatusLinesDirectory),
            settings.WindowsPathOf,
            new ZoomFile(AppPaths.ConversationsPreferencesFile),
            settings.SessionImagesFolder);
        MainWindow = window;
        window.Show();
        _gate.ListenForActivation(() => Dispatcher.BeginInvoke(() => Activate(window)));
        await window.StartAsync().ConfigureAwait(true);
    }

    private static void Activate(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }
        window.Activate();
    }
}
