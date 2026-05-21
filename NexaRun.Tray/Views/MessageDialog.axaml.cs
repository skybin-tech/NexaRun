using Avalonia.Controls;

namespace NexaRun.Tray.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string heading, string body)
    {
        InitializeComponent();
        Title = heading;
        this.FindControl<TextBlock>("HeadingText")!.Text = heading;
        this.FindControl<TextBlock>("BodyText")!.Text = body;
        this.FindControl<Button>("OkBtn")!.Click += (_, _) => Close();
    }

    public static async Task Show(string heading, string body)
    {
        var dlg = new MessageDialog(heading, body);
        await dlg.ShowDialog(GetOwner());
    }

    private static Window GetOwner()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow != null)
            return desktop.MainWindow;

        return new Window { Width = 1, Height = 1, ShowInTaskbar = false };
    }
}
