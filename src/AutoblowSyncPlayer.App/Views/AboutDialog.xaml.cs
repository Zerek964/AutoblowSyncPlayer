using System.Reflection;
using System.Windows;

namespace AutoblowSyncPlayer.App.Views;

/// <summary>
/// About dialog showing version information.
/// </summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var title = $"Autoblow Sync Player v{version}";
        
        Title = title;
    }
}
