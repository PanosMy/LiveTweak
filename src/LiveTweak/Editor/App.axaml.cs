using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;
using AvaloniaApp = Avalonia.Application;

namespace LiveTweak.Editor;

public partial class App : AvaloniaApp
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            SafeLog("UNHANDLED", e.ExceptionObject?.ToString() ?? "null");
        };

        Dispatcher.UIThread.UnhandledException += (s, e) =>
        {
            SafeLog("UI-UNHANDLED", e.Exception.ToString());
            e.Handled = false;
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void SafeLog(string tag, string msg)
    {
        try
        {
            File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "livetweak_log.txt"),
                    $"{DateTime.UtcNow:o} [{tag}] {msg}\n");
        }
        catch { }
    }
}
