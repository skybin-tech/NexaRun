using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NexaRun.Shared.Ipc;
using NexaRun.Tray.Views;

namespace NexaRun.Tray;

public class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private DashboardWindow? _dashboardWindow;
    private readonly IpcClient _ipc = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            BuildTray(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void BuildTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var openItem = new NativeMenuItem("Processes");
        openItem.Click += (_, _) => ShowMainWindow();

        var dashItem = new NativeMenuItem("Dashboard");
        dashItem.Click += (_, _) => ShowDashboard();

        var addItem = new NativeMenuItem("Add Process");
        addItem.Click += (_, _) => ShowAddProcess();

        var separator = new NativeMenuItemSeparator();

        var exitItem = new NativeMenuItem("Exit NexaRun");
        exitItem.Click += (_, _) => desktop.Shutdown();

        var menu = new NativeMenu();
        menu.Items.Add(openItem);
        menu.Items.Add(dashItem);
        menu.Items.Add(addItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "NexaRun",
            Menu = menu,
            IsVisible = true
        };

        _trayIcon.Clicked += (_, _) => ShowDashboard();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsVisible)
        {
            _mainWindow = new MainWindow(_ipc);
            _mainWindow.Show();
        }
        else
        {
            _mainWindow.Activate();
        }
    }

    private void ShowDashboard()
    {
        if (_dashboardWindow == null || !_dashboardWindow.IsVisible)
        {
            _dashboardWindow = new DashboardWindow(_ipc);
            _dashboardWindow.Show();
        }
        else
        {
            _dashboardWindow.Activate();
        }
    }

    private void ShowAddProcess()
    {
        var win = new AddProcessWindow(_ipc);
        win.Show();
    }

    private static WindowIcon CreateIcon()
    {
        // 32x32 terminal icon: dark rounded rectangle with ">_" prompt in green
        var bmp = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(32, 32));
        using (var ctx = bmp.CreateDrawingContext())
        {
            var bg      = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(30, 30, 30));
            var green   = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 220, 110));
            var pen     = new Avalonia.Media.Pen(green, 1.5);

            // Background rounded rect
            ctx.DrawRectangle(bg, null, new Avalonia.Rect(1, 1, 30, 30), 4, 4);

            // ">" chevron at (6, 11)→(11, 16)→(6, 21)
            var chevron = new Avalonia.Media.PathGeometry();
            using (var fig = chevron.Open())
            {
                fig.BeginFigure(new Avalonia.Point(6, 10), false);
                fig.LineTo(new Avalonia.Point(13, 16));
                fig.LineTo(new Avalonia.Point(6, 22));
            }
            ctx.DrawGeometry(null, pen, chevron);

            // "_" underscore cursor at (16, 22)→(24, 22)
            ctx.DrawLine(pen, new Avalonia.Point(16, 22), new Avalonia.Point(24, 22));
        }
        return new WindowIcon(bmp);
    }
}
