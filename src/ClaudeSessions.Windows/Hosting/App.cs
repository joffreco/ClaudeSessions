using System;
using System.Windows;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Application;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Entry point and composition root: wires the adapters to the use cases and the use cases to the windows.</summary>
internal sealed class App : Application, IDisposable
{
    private readonly SingleInstanceGate _gate;
    private KeyboardRouter? _keyboard;
    private WindowCoordinator? _coordinator;

    private App(SingleInstanceGate gate)
    {
        _gate = gate;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <summary>Starts the application, or brings the running instance to the front.</summary>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main()
    {
        SingleInstanceGate? gate = SingleInstanceGate.TryAcquire();
        if (gate is null)
        {
            return 0;
        }
        using (gate)
        {
            using App app = new(gate);
            return app.Run();
        }
    }

    /// <summary>Builds the object graph and opens the saved windows.</summary>
    /// <param name="e">Startup arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppSettings settings = AppSettingsFile.Load(AppPaths.SettingsFile);
        HookFileTabSessionSource sessions = new(settings.SessionTabsDirectory);
        WorkspaceService service = new(
            new JsonWorkspaceStore(AppPaths.WorkspaceFile),
            sessions,
            new SessionSynchronizer(sessions, TimeProvider.System, SessionSynchronizer.DefaultTimeout),
            new GuidIdGenerator(),
            TimeProvider.System,
            settings.DefaultCwd);
        JsonPreferencesStore preferences = new(AppPaths.PreferencesFile);
        WheelScrollService wheel = new(preferences);
        ZoomService zoom = new(preferences, settings.FontSize);
        _coordinator = new WindowCoordinator(service, new WslCommandLine(settings), settings, zoom, wheel);
        _keyboard = new KeyboardRouter(wheel);
        _coordinator.Start();
        WindowCoordinator coordinator = _coordinator;
        _gate.ListenForActivation(() => Dispatcher.BeginInvoke(coordinator.Activate));
    }

    /// <summary>Saves synchronously when Windows ends the session; WPF shuts down right after this returns.</summary>
    /// <param name="e">The session-ending event.</param>
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _coordinator?.OnSessionEnding();
        base.OnSessionEnding(e);
    }

    /// <summary>Stops the keyboard routing.</summary>
    public void Dispose()
    {
        _keyboard?.Dispose();
    }
}
