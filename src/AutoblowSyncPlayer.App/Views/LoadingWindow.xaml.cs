using System.Windows;

namespace AutoblowSyncPlayer.App.Views;

/// <summary>
/// Loading splash screen shown during application startup.
/// </summary>
public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the loading status text.
    /// </summary>
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() => StatusText.Text = status);
    }
}
