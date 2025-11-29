using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LiveTweak.Editor.ViewModels;

namespace LiveTweak.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is not TweakViewModel vm)
            DataContext = vm = new TweakViewModel();

        Opened += (_, __) =>
        {
            Log("Window.Opened -> Refresh");
            Dispatcher.UIThread.Post(() => vm.RefreshCommand.Execute().Subscribe());
        };

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);

    private static void Log(string msg)
    {
        try
        {
            File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "livetweak_log.txt"),
                    $"{DateTime.UtcNow:o} [MainWindow] {msg}\n");
        }
        catch
        {
        }
    }
}
