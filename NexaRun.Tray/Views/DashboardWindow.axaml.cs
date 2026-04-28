using Avalonia.Controls;
using Avalonia.Threading;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using NexaRun.Tray.ViewModels;

namespace NexaRun.Tray.Views;

public partial class DashboardWindow : Window
{
    private readonly IpcClient _ipc;
    private DispatcherTimer? _timer;
    private string? _selectedName;

    private TextBlock _statusText = null!;
    private Button _refreshBtn = null!;
    private ListBox _processList = null!;
    private Border _statsBar = null!;
    private TextBlock _detailStatus = null!, _detailPid = null!, _detailMemory = null!, _detailRestarts = null!;
    private DockPanel _historyPanel = null!;
    private TextBlock _historyTitle = null!;
    private ItemsControl _uptimeBars = null!;
    private DataGrid _runGrid = null!;
    private TextBlock _emptyText = null!;

    public DashboardWindow(IpcClient ipc)
    {
        _ipc = ipc;
        InitializeComponent();

        _statusText    = this.FindControl<TextBlock>("StatusText")!;
        _refreshBtn    = this.FindControl<Button>("RefreshBtn")!;
        _processList   = this.FindControl<ListBox>("ProcessList")!;
        _statsBar      = this.FindControl<Border>("StatsBar")!;
        _detailStatus  = this.FindControl<TextBlock>("DetailStatus")!;
        _detailPid     = this.FindControl<TextBlock>("DetailPid")!;
        _detailMemory  = this.FindControl<TextBlock>("DetailMemory")!;
        _detailRestarts = this.FindControl<TextBlock>("DetailRestarts")!;
        _historyPanel  = this.FindControl<DockPanel>("HistoryPanel")!;
        _historyTitle  = this.FindControl<TextBlock>("HistoryTitle")!;
        _uptimeBars    = this.FindControl<ItemsControl>("UptimeBars")!;
        _runGrid       = this.FindControl<DataGrid>("RunGrid")!;
        _emptyText     = this.FindControl<TextBlock>("EmptyText")!;

        WireEvents();
    }

    private void WireEvents()
    {
        _refreshBtn.Click += async (_, _) => await RefreshProcesses();

        _processList.SelectionChanged += async (_, _) =>
        {
            var item = _processList.SelectedItem as DashboardProcessItem;
            _selectedName = item?.Name;
            if (item != null)
                await ShowDetail(item);
            else
                ShowEmpty();
        };

        Opened  += async (_, _) => { await RefreshProcesses(); StartTimer(); };
        Closing += (_, _) => _timer?.Stop();
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (_, _) =>
        {
            await RefreshProcesses();
            // Re-load history if a process is selected (stats change)
            if (_selectedName != null)
            {
                var item = (_processList.ItemsSource as List<DashboardProcessItem>)
                    ?.FirstOrDefault(x => x.Name == _selectedName);
                if (item != null) await ShowDetail(item);
            }
        };
        _timer.Start();
    }

    private async Task RefreshProcesses()
    {
        var response = await _ipc.Send(new IpcRequest { Command = "list" });
        _statusText.Text = response.Success ? string.Empty : $"⚠ {response.Message}";
        if (!response.Success) return;

        var items = (response.Processes ?? [])
            .Select(p => new DashboardProcessItem(p))
            .ToList();

        var prevSelected = _selectedName;
        _processList.ItemsSource = items;

        // Restore selection
        if (prevSelected != null)
        {
            var match = items.FirstOrDefault(x => x.Name == prevSelected);
            if (match != null) _processList.SelectedItem = match;
        }
    }

    private async Task ShowDetail(DashboardProcessItem item)
    {
        // Update stats bar
        _detailStatus.Text  = item.StatusLabel;
        _detailStatus.Foreground = Avalonia.Media.SolidColorBrush.Parse(item.StatusColor);
        _detailPid.Text      = item.Pid;
        _detailMemory.Text   = item.Memory;
        _detailRestarts.Text = item.Restarts.ToString();
        _statsBar.IsVisible  = true;
        _emptyText.IsVisible = false;

        // Fetch run history
        var resp = await _ipc.Send(new IpcRequest { Command = "history", ProcessName = item.Name });
        var history = resp.RunHistory ?? [];

        _historyTitle.Text    = item.Name;
        _historyPanel.IsVisible = true;

        // Build 7-day uptime bars
        var bars = BuildUptimeBars(history);
        _uptimeBars.ItemsSource = bars;

        // Populate run grid
        _runGrid.ItemsSource = history.Select(r => new RunHistoryRow(r)).ToList();
    }

    private void ShowEmpty()
    {
        _statsBar.IsVisible     = false;
        _historyPanel.IsVisible = false;
        _emptyText.IsVisible    = true;
    }

    private static List<UptimeDayBar> BuildUptimeBars(List<ProcessRun> history)
    {
        var bars = new List<UptimeDayBar>();
        var today = DateTime.UtcNow.Date;

        for (int i = 6; i >= 0; i--)
        {
            var day     = today.AddDays(-i);
            var dayEnd  = day.AddDays(1);
            var seconds = 86400.0;

            // Sum time the process was running during this calendar day
            double uptimeSec = 0;
            foreach (var run in history)
            {
                var runStart = run.StartedAt > day     ? run.StartedAt : day;
                var runEnd   = (run.EndedAt ?? DateTime.UtcNow) < dayEnd
                             ? (run.EndedAt ?? DateTime.UtcNow) : dayEnd;
                if (runEnd > runStart)
                    uptimeSec += (runEnd - runStart).TotalSeconds;
            }

            bars.Add(new UptimeDayBar(day, Math.Min(uptimeSec / seconds, 1.0)));
        }

        return bars;
    }
}
