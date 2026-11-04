using System;
using System.Linq;
using System.Windows;
using ClaudeSessions.Adapters;
using ClaudeSessions.Adapters.Consignes;
using ClaudeSessions.Consignes.Views;

namespace ClaudeSessions.Consignes.Hosting;

/// <summary>Entry point and composition root: wires the adapters to the controller and the controller to the window.</summary>
internal sealed class App : Application
{
    private const string FluentTheme = "pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml";

    private readonly SingleInstanceGate _gate;

    private App(SingleInstanceGate gate)
    {
        _gate = gate;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    /// <summary>Starts the application, brings the running instance to the front, or runs the install check.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains(InstallCheck.Argument))
        {
            return InstallCheck.Run();
        }
        SingleInstanceGate? gate = SingleInstanceGate.TryAcquire(SingleInstanceGate.Consignes);
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
        BoardController controller = new(
            new FolderConsigneSource(settings.ConsignesDirectory),
            new WorkspaceTabDirectory(AppPaths.WorkspaceFile),
            new JsonReadingStateStore(AppPaths.ConsignesStateFile),
            TimeSpan.FromSeconds(Math.Max(settings.ConsignesPollSeconds, 1)));
        MainWindow window = new(controller);
        MainWindow = window;
        window.Show();
        _gate.ListenForActivation(() => Dispatcher.BeginInvoke(() => Activate(window)));
        await controller.StartAsync(window).ConfigureAwait(true);
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
