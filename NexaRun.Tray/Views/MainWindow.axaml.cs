using Avalonia.Controls;
using Avalonia.Threading;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using NexaRun.Tray.ViewModels;

namespace NexaRun.Tray.Views;

public partial class MainWindow : Window
{
    private readonly IpcClient _ipc;
    private DispatcherTimer? _timer;
    private ProcessRow? _selected;

    private DataGrid _processGrid = null!;
    private Button _addBtn = null!, _refreshBtn = null!, _stopBtn = null!, _restartBtn = null!, _logsBtn = null!, _editBtn = null!, _deleteBtn = null!;
    private TextBlock _statusText = null!;

    public MainWindow(IpcClient ipc)
    {
        _ipc = ipc;
        InitializeComponent();

        _processGrid = this.FindControl<DataGrid>("ProcessGrid")!;
        _addBtn      = this.FindControl<Button>("AddBtn")!;
        _refreshBtn  = this.FindControl<Button>("RefreshBtn")!;
        _stopBtn     = this.FindControl<Button>("StopBtn")!;
        _restartBtn  = this.FindControl<Button>("RestartBtn")!;
        _logsBtn     = this.FindControl<Button>("LogsBtn")!;
        _editBtn     = this.FindControl<Button>("EditBtn")!;
        _deleteBtn   = this.FindControl<Button>("DeleteBtn")!;
        _statusText  = this.FindControl<TextBlock>("StatusText")!;

        WireEvents();
    }

    private void WireEvents()
    {
        _processGrid.SelectionChanged += (_, _) =>
        {
            _selected = _processGrid.SelectedItem as ProcessRow;
            var has = _selected != null;
            _stopBtn.IsEnabled    = has && _selected!.IsOnline;
            _restartBtn.IsEnabled = has;
            _logsBtn.IsEnabled    = has;
            _editBtn.IsEnabled    = has;
            _deleteBtn.IsEnabled  = has;
        };

        _addBtn.Click     += (_, _) => new AddProcessWindow(_ipc).Show();
        _refreshBtn.Click += async (_, _) => await Refresh();
        _stopBtn.Click    += async (_, _) => await SendAction("stop", _selected?.Name);
        _restartBtn.Click += async (_, _) => await SendAction("restart", _selected?.Name);
        _editBtn.Click    += (_, _) => OpenEdit();
        _deleteBtn.Click  += async (_, _) => await ConfirmDelete();
        _logsBtn.Click    += (_, _) => OpenLogs();

        Opened  += async (_, _) => { await Refresh(); StartTimer(); };
        Closing += (_, _) => _timer?.Stop();
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += async (_, _) => await Refresh();
        _timer.Start();
    }

    private async Task Refresh()
    {
        var response = await _ipc.Send(new IpcRequest { Command = "list" });
        _statusText.Text = response.Success ? string.Empty : $"⚠ {response.Message}";
        if (!response.Success) return;

        var rows = (response.Processes ?? []).Select(p => new ProcessRow(p)).ToList();
        _processGrid.ItemsSource = rows;
    }

    private async Task SendAction(string command, string? name)
    {
        if (string.IsNullOrEmpty(name)) return;
        await _ipc.Send(new IpcRequest { Command = command, ProcessName = name });
        await Refresh();
    }

    private void OpenEdit()
    {
        if (_selected == null) return;
        new AddProcessWindow(_ipc, _selected.Source).Show();
    }

    private async Task ConfirmDelete()
    {
        if (_selected == null) return;
        var dlg = new ConfirmDialog($"Delete '{_selected.Name}'?", "This will remove it from the process list.");
        var confirmed = await dlg.ShowDialog<bool>(this);
        if (confirmed) await SendAction("delete", _selected.Name);
    }

    private void OpenLogs()
    {
        if (_selected == null) return;
        new LogsWindow(_ipc, _selected.Name).Show();
    }
}
