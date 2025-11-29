using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaApp = Avalonia.Application;


namespace LiveTweak.Editor;

public static class TweakEditorApp
{
    private static int _started;

    public static void Start(CancellationToken token)
    {
        // Prevent multiple launches
        if (Interlocked.Exchange(ref _started, 1) == 1)
            return;

        // If Avalonia already running, just open a MainWindow
        if (AvaloniaApp.Current is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var win = new MainWindow();
                win.Show();
                token.Register(() => Dispatcher.UIThread.Post(win.Close));
            });
            return;
        }

        // Start Avalonia with App + MainWindow on an STA UI thread
        var startupThread = new Thread(() =>
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime
            {
                ShutdownMode = ShutdownMode.OnLastWindowClose
            };

            AppBuilder
                    .Configure<App>()
                    .UsePlatformDetect()
                    .LogToTrace()
                    .SetupWithLifetime(lifetime);

            // App.OnFrameworkInitializationCompleted will create MainWindow
            token.Register(() =>
            {
                if (lifetime.MainWindow is { } w)
                    Dispatcher.UIThread.Post(w.Close);
            });

            lifetime.Start([]);
            return;
        })
        {
            IsBackground = true,
            Name = "LiveTweakUI",
        };

        startupThread.TrySetApartmentState(ApartmentState.STA);
        startupThread.Start();
    }
}
