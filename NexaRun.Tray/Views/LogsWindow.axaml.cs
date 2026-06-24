using Avalonia.Controls;
using Avalonia.Threading;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Tray.Views;

public partial class LogsWindow : Window
{
    private IpcClient _ipc = null!;
    private string _processName = "";
    private DispatcherTimer? _timer;

    private TextBlock _titleText = null!;
    private Button _refreshBtn = null!;
    private Button _clearLogBtn = null!;
    private CheckBox _autoScrollBox = null!;
    private ScrollViewer _scroller = null!;
    private TextBox _logBox = null!;

    public LogsWindow()
    {
        InitializeComponent();
    }

    public LogsWindow(IpcClient ipc, string processName) : this()
    {
        _ipc = ipc;
        _processName = processName;

        _titleText    = this.FindControl<TextBlock>("TitleText")!;
        _refreshBtn   = this.FindControl<Button>("RefreshBtn")!;
        _clearLogBtn  = this.FindControl<Button>("ClearLogBtn")!;
        _autoScrollBox= this.FindControl<CheckBox>("AutoScrollBox")!;
        _scroller     = this.FindControl<ScrollViewer>("Scroller")!;
        _logBox       = this.FindControl<TextBox>("LogBox")!;

        Title = $"Logs — {processName}";
        _titleText.Text = processName;

        _refreshBtn.Click += async (_, _) => await Refresh();
        _clearLogBtn.Click += async (_, _) => await ClearLog();
        Opened  += async (_, _) => { await Refresh(); StartTimer(); };
        Closing += (_, _) => _timer?.Stop();
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await Refresh();
        _timer.Start();
    }

    private async Task Refresh()
    {
        var response = await _ipc.Send(new IpcRequest
        {
            Command = "logs",
            ProcessName = _processName,
            LogLines = 200
        });

        if (!response.Success) return;

        _logBox.Text = response.Logs ?? string.Empty;

        if (_autoScrollBox.IsChecked == true)
            _scroller.ScrollToEnd();
    }

    private async Task ClearLog()
    {
        var dlg = new ConfirmDialog(
            $"Clear logs for '{_processName}'?",
            "Deletes the on-disk log files for this process. New output will continue to be captured.");
        var confirmed = await dlg.ShowDialog<bool>(this);
        if (!confirmed) return;

        var response = await _ipc.Send(new IpcRequest
        {
            Command = "clear-logs",
            ProcessName = _processName
        });

        if (!response.Success)
        {
            _logBox.Text = response.Message;
            return;
        }

        await Refresh();
    }
}
