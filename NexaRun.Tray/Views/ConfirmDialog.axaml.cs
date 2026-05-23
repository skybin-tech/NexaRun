using Avalonia.Controls;

namespace NexaRun.Tray.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string heading, string body) : this()
    {

        Title = heading;
        this.FindControl<TextBlock>("HeadingText")!.Text = heading;
        this.FindControl<TextBlock>("BodyText")!.Text = body;
        this.FindControl<Button>("ConfirmBtn")!.Click += (_, _) => Close(true);
        this.FindControl<Button>("CancelBtn")!.Click  += (_, _) => Close(false);
    }
}
