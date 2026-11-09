using System;
using System.Windows;
using ClaudeSessions.Adapters;
using ClaudeSessions.Core.Application;
using ClaudeSessions.Windows.Views;
using Microsoft.Win32;

namespace ClaudeSessions.Windows.Hosting;

/// <summary>Entry point and composition root: wires the adapters to the use cases and the use cases to the windows.</summary>
internal sealed class App : Application, IDisposable
{
    private readonly SingleInstanceGate _gate;
    private KeyboardRouter? _keyboard;
    private WindowCoordinator? _coordinator;
    private SessionRecordWatcher? _records;

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
        SingleInstanceGate? gate = SingleInstanceGate.TryAcquire(SingleInstanceGate.ClaudeSessions);
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
        AppSettings settings = AppSettingsFile.Load(AppPaths.SettingsFile, WslDefaultDistribution.Read());
        Environment.SetEnvironmentVariable(WslEnvironment.RecordsVariable, AppPaths.SessionRecordsFolder);
        Environment.SetEnvironmentVariable(WslEnvironment.ForwardVariable, WslEnvironment.ForwardRecords(Environment.GetEnvironmentVariable(WslEnvironment.ForwardVariable)));
        HookFileTabSessionSource sessions = new(AppPaths.SessionRecordsFolder);
        WorkspaceService service = new(
            new JsonWorkspaceStore(AppPaths.WorkspaceFile),
            sessions,
            new SessionSynchronizer(sessions, TimeProvider.System, SessionSynchronizer.DefaultTimeout),
            new GuidIdGenerator(),
            TimeProvider.System,
            settings.DefaultCwd);
        AppTheme.Apply(WindowsAppMode.IsDark());
        JsonPreferencesStore preferences = new(AppPaths.PreferencesFile);
        WheelScrollService wheel = new(preferences);
        ZoomService zoom = new(preferences, settings.FontSize);
        WslCommandLine commandLine = new(settings);
        IdleSuspensionService sleep = new(preferences, new WslTabActivityProbe(settings.WslExecutable, commandLine), TimeProvider.System);
        _coordinator = new WindowCoordinator(service, commandLine, settings, zoom, wheel, sleep);
        _keyboard = new KeyboardRouter(wheel, _coordinator.ClearBadges);
        _coordinator.Start();
        WindowCoordinator coordinator = _coordinator;
        _records = new SessionRecordWatcher(AppPaths.SessionRecordsFolder);
        _records.RecordChanged += tabId => Dispatcher.BeginInvoke(() => coordinator.OnSessionRecordChanged(tabId));
        _records.ApprovalRequested += tabId => Dispatcher.BeginInvoke(() => coordinator.OnApprovalRequested(tabId));
        _gate.ListenForActivation(() => Dispatcher.BeginInvoke(coordinator.Activate));
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.BeginInvoke(() => AppTheme.Apply(WindowsAppMode.IsDark()));
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.RemoteConnect or SessionSwitchReason.ConsoleConnect && _coordinator is WindowCoordinator coordinator)
        {
            Dispatcher.BeginInvoke(coordinator.RedrawAfterReconnect);
        }
    }

    /// <summary>Saves synchronously when Windows ends the session; WPF shuts down right after this returns.</summary>
    /// <param name="e">The session-ending event.</param>
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _coordinator?.OnSessionEnding();
        base.OnSessionEnding(e);
    }

    /// <summary>Stops the keyboard routing, the session switch and theme listening and the watching of the hook records.</summary>
    public void Dispose()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _keyboard?.Dispose();
        _records?.Dispose();
    }
}
