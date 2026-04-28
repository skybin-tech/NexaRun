namespace NexaRun.Tray.ViewModels;

public class UptimeDayBar(DateTime day, double uptimeFraction)
{
    public string DayLabel { get; } = day.ToString("MMM d");
    public string UptimePct { get; } = $"{uptimeFraction * 100:F0}%";
    public double BarHeight { get; } = Math.Max(2, uptimeFraction * 40);
    public string BarColor { get; } = uptimeFraction >= 0.95 ? "#22c55e"
                                    : uptimeFraction >= 0.5  ? "#eab308"
                                    :                          "#ef4444";
}
