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
    private StackPanel _detailTabs = null!;
    private Button _historyTabBtn = null!, _logsTabBtn = null!;
    private DockPanel _logsPanel = null!;
    private TextBlock _logsTitle = null!;
    private Button _logsRefreshBtn = null!;
    private CheckBox _logsAutoScrollBox = null!;
    private CheckBox _logsLiveBox = null!;
    private ScrollViewer _logsScroller = null!;
    private TextBox _logsBox = null!;

    private enum DetailView { History, Logs }
    private DetailView _detailView = DetailView.History;

    private static readonly TimeSpan ListRefreshInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LogsLiveInterval = TimeSpan.FromSeconds(8);

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
        _detailTabs    = this.FindControl<StackPanel>("DetailTabs")!;
        _historyTabBtn = this.FindControl<Button>("HistoryTabBtn")!;
        _logsTabBtn    = this.FindControl<Button>("LogsTabBtn")!;
        _logsPanel     = this.FindControl<DockPanel>("LogsPanel")!;
        _logsTitle     = this.FindControl<TextBlock>("LogsTitle")!;
        _logsRefreshBtn = this.FindControl<Button>("LogsRefreshBtn")!;
        _logsAutoScrollBox = this.FindControl<CheckBox>("LogsAutoScrollBox")!;
        _logsLiveBox     = this.FindControl<CheckBox>("LogsLiveBox")!;
        _logsScroller  = this.FindControl<ScrollViewer>("LogsScroller")!;
        _logsBox       = this.FindControl<TextBox>("LogsBox")!;

        WireEvents();
    }

    private void WireEvents()
    {
        _refreshBtn.Click += async (_, _) => await RefreshAll();
        _historyTabBtn.Click += async (_, _) => await ShowHistoryTab();
        _logsTabBtn.Click += async (_, _) => await ShowLogsTab();
        _logsRefreshBtn.Click += async (_, _) => await RefreshLogs();
        _logsLiveBox.IsCheckedChanged += (_, _) =>
        {
            if (_timer != null && _detailView == DetailView.Logs)
                _timer.Interval = _logsLiveBox.IsChecked == true ? LogsLiveInterval : ListRefreshInterval;
        };

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
        _timer = new DispatcherTimer { Interval = ListRefreshInterval };
        _timer.Tick += async (_, _) => await OnBackgroundTick();
        _timer.Start();
    }

    private async Task OnBackgroundTick()
    {
        await RefreshProcesses();

        // Stats only — do not reload history grid or uptime chart
        UpdateSelectedStats();

        if (_detailView == DetailView.Logs && _logsLiveBox.IsChecked == true)
            await RefreshLogs();
    }

    private async Task RefreshAll()
    {
        await RefreshProcesses();
        if (_selectedName == null) return;

        var item = FindSelectedItem();
        if (item == null) return;

        if (_detailView == DetailView.Logs)
            await RefreshLogs();
        else
            await ShowDetail(item);
    }

    private DashboardProcessItem? FindSelectedItem() =>
        (_processList.ItemsSource as List<DashboardProcessItem>)
            ?.FirstOrDefault(x => x.Name == _selectedName)
        ?? _processList.SelectedItem as DashboardProcessItem;

    private void UpdateSelectedStats()
    {
        var item = FindSelectedItem();
        if (item == null) return;

        _detailStatus.Text = item.StatusLabel;
        _detailStatus.Foreground = Avalonia.Media.SolidColorBrush.Parse(item.StatusColor);
        _detailPid.Text = item.Pid;
        _detailMemory.Text = item.Memory;
        _detailRestarts.Text = item.Restarts.ToString();
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

        // Restore selection without re-firing change when same item
        if (prevSelected != null)
        {
            var match = items.FirstOrDefault(x => x.Name == prevSelected);
            if (match != null && (_processList.SelectedItem as DashboardProcessItem)?.Name != match.Name)
                _processList.SelectedItem = match;
        }
    }

    private async Task ShowDetail(DashboardProcessItem item)
    {
        UpdateSelectedStats();
        _statsBar.IsVisible  = true;
        _emptyText.IsVisible = false;

        // Fetch run history
        var resp = await _ipc.Send(new IpcRequest { Command = "history", ProcessName = item.Name });
        var history = resp.RunHistory ?? [];

        _historyTitle.Text = item.Name;
        _logsTitle.Text = $"{item.Name} — log output";
        ApplyDetailView();

        // Build 7-day uptime bars
        var bars = BuildUptimeBars(history);
        _uptimeBars.ItemsSource = bars;

        // Populate run grid
        _runGrid.ItemsSource = history.Select(r => new RunHistoryRow(r)).ToList();

        if (_detailView == DetailView.Logs)
            await RefreshLogs();
    }

    private void ShowEmpty()
    {
        _statsBar.IsVisible     = false;
        _detailTabs.IsVisible   = false;
        _historyPanel.IsVisible = false;
        _logsPanel.IsVisible    = false;
        _emptyText.IsVisible    = true;
    }

    private async Task ShowHistoryTab()
    {
        _detailView = DetailView.History;
        ApplyDetailView();
        if (_timer != null)
            _timer.Interval = ListRefreshInterval;
        var item = _processList.SelectedItem as DashboardProcessItem;
        if (item != null)
            await ShowDetail(item);
    }

    private async Task ShowLogsTab()
    {
        _detailView = DetailView.Logs;
        ApplyDetailView();
        if (_timer != null)
            _timer.Interval = _logsLiveBox.IsChecked == true ? LogsLiveInterval : ListRefreshInterval;
        await RefreshLogs();
    }

    private void ApplyDetailView()
    {
        var hasSelection = _processList.SelectedItem != null;
        _detailTabs.IsVisible = hasSelection;
        _emptyText.IsVisible = !hasSelection;

        var history = _detailView == DetailView.History;
        _historyPanel.IsVisible = hasSelection && history;
        _logsPanel.IsVisible = hasSelection && !history;
    }

    private async Task RefreshLogs()
    {
        if (string.IsNullOrEmpty(_selectedName)) return;

        var response = await _ipc.Send(new IpcRequest
        {
            Command = "logs",
            ProcessName = _selectedName,
            LogLines = 300
        });

        if (!response.Success)
        {
            _logsBox.Text = response.Message;
            return;
        }

        _logsBox.Text = response.Logs ?? string.Empty;
        if (_logsAutoScrollBox.IsChecked == true)
            _logsScroller.ScrollToEnd();
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
