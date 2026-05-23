using Avalonia.Controls;
using Avalonia.Threading;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using NexaRun.Tray.Services;
using NexaRun.Tray.ViewModels;

namespace NexaRun.Tray.Views;

public partial class MainWindow : Window
{
    private IpcClient _ipc = null!;
    private TrayConfigService _config = null!;
    private DispatcherTimer? _timer;
    private ProcessRow? _selected;
    private string? _selectedName;

    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(45);

    private DataGrid _processGrid = null!;
    private Button _addBtn = null!, _importBtn = null!, _exportBtn = null!, _clearAllBtn = null!, _settingsBtn = null!, _refreshBtn = null!;
    private Button _stopAllBtn = null!, _restartAllBtn = null!, _openUrlBtn = null!;
    private Button _stopBtn = null!, _restartBtn = null!, _logsBtn = null!, _editBtn = null!, _deleteBtn = null!;
    private TextBlock _statusText = null!;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IpcClient ipc) : this()
    {
        _ipc = ipc;
        _config = new TrayConfigService(ipc);

        _processGrid = this.FindControl<DataGrid>("ProcessGrid")!;
        _addBtn      = this.FindControl<Button>("AddBtn")!;
        _importBtn   = this.FindControl<Button>("ImportBtn")!;
        _exportBtn   = this.FindControl<Button>("ExportBtn")!;
        _clearAllBtn = this.FindControl<Button>("ClearAllBtn")!;
        _settingsBtn = this.FindControl<Button>("SettingsBtn")!;
        _refreshBtn  = this.FindControl<Button>("RefreshBtn")!;
        _stopAllBtn    = this.FindControl<Button>("StopAllBtn")!;
        _restartAllBtn = this.FindControl<Button>("RestartAllBtn")!;
        _openUrlBtn    = this.FindControl<Button>("OpenUrlBtn")!;
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
            _selectedName = _selected?.Name;
            ApplySelection(_selected);
        };

        _addBtn.Click     += (_, _) => new AddProcessWindow(_ipc).Show();
        _importBtn.Click  += async (_, _) => await TrayImportExportActions.ImportFromPicker(this, _config, () => Refresh(preserveSelection: true));
        _exportBtn.Click  += async (_, _) => await TrayImportExportActions.ExportToPicker(this, _config);
        _clearAllBtn.Click += async (_, _) => await ConfirmClearAll();
        _settingsBtn.Click += (_, _) => new SettingsWindow(_ipc).Show();
        _refreshBtn.Click += async (_, _) => await Refresh(preserveSelection: true);
        _stopAllBtn.Click    += async (_, _) => await StopAll();
        _restartAllBtn.Click += async (_, _) => await RestartAll();
        _openUrlBtn.Click    += (_, _) => OpenSelectedUrl();
        _stopBtn.Click    += async (_, _) => await SendAction("stop");
        _restartBtn.Click += async (_, _) => await SendAction("restart");
        _editBtn.Click    += (_, _) => OpenEdit();
        _deleteBtn.Click  += async (_, _) => await ConfirmDelete();
        _logsBtn.Click    += (_, _) => OpenLogs();

        Opened  += async (_, _) => { await Refresh(preserveSelection: false); StartTimer(); };
        Closing += (_, _) => _timer?.Stop();
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = AutoRefreshInterval };
        _timer.Tick += async (_, _) => await Refresh(preserveSelection: true);
        _timer.Start();
    }

    private void ApplySelection(ProcessRow? row)
    {
        var has = row != null;
        _stopBtn.IsEnabled    = has && row!.IsOnline;
        _restartBtn.IsEnabled = has;
        _openUrlBtn.IsEnabled = has && row!.HasUrl;
        _logsBtn.IsEnabled    = has;
        _editBtn.IsEnabled    = has;
        _deleteBtn.IsEnabled  = has;
    }

    public async Task RefreshFromTray() => await Refresh(preserveSelection: true);

    private async Task Refresh(bool preserveSelection = false)
    {
        var keepName = preserveSelection ? _selectedName : null;

        var response = await _ipc.Send(new IpcRequest { Command = "list" });
        if (!response.Success)
        {
            _statusText.Text = $"⚠ {response.Message}";
            return;
        }

        if (!preserveSelection)
            _statusText.Text = string.Empty;

        var rows = (response.Processes ?? []).Select(p => new ProcessRow(p)).ToList();
        _processGrid.ItemsSource = rows;
        _stopAllBtn.IsEnabled = rows.Any(r => r.IsOnline);
        _restartAllBtn.IsEnabled = rows.Count > 0;
        _clearAllBtn.IsEnabled = rows.Count > 0;

        if (!string.IsNullOrEmpty(keepName))
        {
            var row = rows.FirstOrDefault(r => r.Name == keepName);
            if (row != null)
            {
                _processGrid.SelectedItem = row;
                _selected = row;
                _selectedName = row.Name;
                ApplySelection(row);
                return;
            }
        }

        _selected = null;
        if (!preserveSelection)
            _selectedName = null;
        ApplySelection(null);
    }

    private async Task StopAll()
    {
        var response = await _ipc.Send(new IpcRequest { Command = "list" });
        if (!response.Success)
        {
            _statusText.Text = $"⚠ {response.Message}";
            return;
        }

        var online = (response.Processes ?? []).Where(p => p.Status == ProcessStatus.Online).ToList();
        if (online.Count == 0)
        {
            _statusText.Text = "No running processes to stop.";
            return;
        }

        _stopAllBtn.IsEnabled = false;
        _statusText.Text = $"Stopping {online.Count} processes…";
        try
        {
            foreach (var p in online)
                await _ipc.Send(new IpcRequest { Command = "stop", ProcessName = p.Name });

            _statusText.Text = $"Stopped {online.Count} processes.";
        }
        finally
        {
            await Refresh(preserveSelection: true);
        }
    }

    private void OpenSelectedUrl()
    {
        var url = _selected?.Source.Url;
        if (!BrowserHelper.TryOpen(url))
            _statusText.Text = string.IsNullOrWhiteSpace(url)
                ? "No URL configured for this process."
                : $"⚠ Invalid URL: {url}";
    }

    private async Task RestartAll()
    {
        var list = await _ipc.Send(new IpcRequest { Command = "list" });
        if (!list.Success)
        {
            _statusText.Text = $"⚠ {list.Message}";
            return;
        }

        var count = list.Processes?.Count ?? 0;
        if (count == 0)
        {
            _statusText.Text = "No processes to restart.";
            return;
        }

        _timer?.Stop();
        _restartAllBtn.IsEnabled = false;
        _statusText.Text = $"Restart All: restarting {count} processes one by one…";
        try
        {
            var response = await _ipc.Send(new IpcRequest { Command = "restart-all" });
            _statusText.Text = response.Success
                ? response.Message
                : $"⚠ {response.Message}";
        }
        finally
        {
            _timer?.Start();
            _selected = null;
            _selectedName = null;
            await Refresh(preserveSelection: false);
        }
    }

    private async Task SendAction(string command)
    {
        var name = _selected?.Name ?? _selectedName;
        if (string.IsNullOrEmpty(name)) return;
        await _ipc.Send(new IpcRequest { Command = command, ProcessName = name });
        await Refresh(preserveSelection: true);
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
        if (confirmed) await SendAction("delete");
    }

    private async Task ConfirmClearAll()
    {
        var response = await _ipc.Send(new IpcRequest { Command = "list" });
        if (!response.Success)
        {
            _statusText.Text = $"⚠ {response.Message}";
            return;
        }

        var count = response.Processes?.Count ?? 0;
        if (count == 0)
        {
            _statusText.Text = "Process list is already empty.";
            return;
        }

        var dlg = new ConfirmDialog(
            "Clear all processes?",
            $"Stop any running apps, remove all {count} entries, and save an empty list to %APPDATA%\\NexaRun\\processes.json. Run Import JSON to load them again.");
        var confirmed = await dlg.ShowDialog<bool>(this);
        if (!confirmed) return;

        _clearAllBtn.IsEnabled = false;
        var clear = await _ipc.Send(new IpcRequest { Command = "clear-all" });
        _statusText.Text = clear.Success ? clear.Message : $"⚠ {clear.Message}";
        _selected = null;
        _selectedName = null;
        await Refresh(preserveSelection: false);
    }

    private void OpenLogs()
    {
        var name = _selected?.Name ?? _selectedName;
        if (string.IsNullOrEmpty(name)) return;
        new LogsWindow(_ipc, name).Show();
    }
}
