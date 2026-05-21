using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NexaRun.Shared;
using NexaRun.Shared.Ipc;
using NexaRun.Tray.Services;
using NexaRun.Tray.Views;

namespace NexaRun.Tray;

public class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private DashboardWindow? _dashboardWindow;
    private readonly IpcClient _ipc = new();
    private TrayConfigService? _config;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        NexaRunPaths.EnsureDirectories();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _config = new TrayConfigService(_ipc);
            BuildTray(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void BuildTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();

        AddMenuItem(menu, "Processes", (_, _) => ShowMainWindow());
        AddMenuItem(menu, "Dashboard", (_, _) => ShowDashboard());
        AddMenuItem(menu, "Add Process", (_, _) => ShowAddProcess());

        var importSub = new NativeMenu();
        AddMenuItem(importSub, "Import JSON file...", async (_, _) => await ImportJson());
        AddMenuItem(importSub, "Import nexarun-processes.json", async (_, _) => await ImportBundledJson());

        menu.Items.Add(new NativeMenuItem("Import JSON") { Menu = importSub });

        AddMenuItem(menu, "Export JSON...", async (_, _) => await ExportJson());

        menu.Items.Add(new NativeMenuItemSeparator());
        AddMenuItem(menu, "Open data folder", (_, _) => OpenDataFolder());
        AddMenuItem(menu, "Exit NexaRun", (_, _) => desktop.Shutdown());

        _trayIcon = new TrayIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "NexaRun — right-click for menu",
            IsVisible = true,
            Menu = menu
        };

        _trayIcon.Clicked += (_, _) => ShowDashboard();
    }

    private static void AddMenuItem(NativeMenu menu, string header, EventHandler click)
    {
        var item = new NativeMenuItem(header);
        item.Click += click;
        menu.Items.Add(item);
    }

    private async Task ImportBundledJson()
    {
        if (_config == null) return;
        await TrayImportExportActions.ImportBundledJson(_mainWindow, _config, RefreshMainWindow);
    }

    private async Task ImportJson()
    {
        if (_config == null) return;
        await TrayImportExportActions.ImportFromPicker(_mainWindow, _config, RefreshMainWindow);
    }

    private async Task ExportJson()
    {
        if (_config == null) return;
        await TrayImportExportActions.ExportToPicker(_mainWindow, _config);
    }

    private static void OpenDataFolder()
    {
        NexaRunPaths.EnsureDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = NexaRunPaths.DataDir,
            UseShellExecute = true
        });
    }

    private Task RefreshMainWindow() =>
        _mainWindow?.IsVisible == true ? _mainWindow.RefreshFromTray() : Task.CompletedTask;

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
        new AddProcessWindow(_ipc).Show();
    }

    private static WindowIcon CreateIcon()
    {
        var bmp = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(32, 32));
        using (var ctx = bmp.CreateDrawingContext())
        {
            var bg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(30, 30, 30));
            var green = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 220, 110));
            var pen = new Avalonia.Media.Pen(green, 1.5);

            ctx.DrawRectangle(bg, null, new Avalonia.Rect(1, 1, 30, 30), 4, 4);

            var chevron = new Avalonia.Media.PathGeometry();
            using (var fig = chevron.Open())
            {
                fig.BeginFigure(new Avalonia.Point(6, 10), false);
                fig.LineTo(new Avalonia.Point(13, 16));
                fig.LineTo(new Avalonia.Point(6, 22));
            }
            ctx.DrawGeometry(null, pen, chevron);
            ctx.DrawLine(pen, new Avalonia.Point(16, 22), new Avalonia.Point(24, 22));
        }
        return new WindowIcon(bmp);
    }
}
