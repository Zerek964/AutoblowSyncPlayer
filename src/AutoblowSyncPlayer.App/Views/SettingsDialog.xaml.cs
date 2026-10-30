using System.IO;
using System.Windows;
using AutoblowSyncPlayer.Core.Services;

namespace AutoblowSyncPlayer.App.Views;

/// <summary>
/// Settings dialog for configuring application preferences.
/// </summary>
public partial class SettingsDialog : Window
{
    private readonly ISettingsService _settingsService;

    public SettingsDialog(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        // Show settings file location
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoblowSyncPlayer");

        TxtSettingsPath.Text = Path.Combine(appDataFolder, "settings.json");

        // Load current settings
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            ChkAutoDetectScript.IsChecked = _settingsService.AutoDetectScript;
            ChkUploadConfirmation.IsChecked = _settingsService.UploadConfirmation;
            ChkLoggingEnabled.IsChecked = _settingsService.LoggingEnabled;
            TxtDefaultOffset.Text = _settingsService.SyncOffsetMs.ToString();
            ChkResumeLastPosition.IsChecked = _settingsService.ResumeLastPosition;
            TxtPollingInterval.Text = _settingsService.PollingIntervalMs.ToString();
            ChkRememberWindowSize.IsChecked = _settingsService.RememberWindowSize;
            ChkStartMinimized.IsChecked = _settingsService.StartMinimized;
        }
        catch
        {
            // Use defaults on error
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save settings
            _settingsService.AutoDetectScript = ChkAutoDetectScript.IsChecked == true;
            _settingsService.UploadConfirmation = ChkUploadConfirmation.IsChecked == true;
            _settingsService.LoggingEnabled = ChkLoggingEnabled.IsChecked == true;
            _settingsService.ResumeLastPosition = ChkResumeLastPosition.IsChecked == true;
            _settingsService.RememberWindowSize = ChkRememberWindowSize.IsChecked == true;
            _settingsService.StartMinimized = ChkStartMinimized.IsChecked == true;
            
            if (int.TryParse(TxtDefaultOffset.Text, out int offset))
            {
                _settingsService.SyncOffsetMs = offset;
            }
            
            if (int.TryParse(TxtPollingInterval.Text, out int pollingInterval))
            {
                _settingsService.PollingIntervalMs = Math.Max(1000, Math.Min(pollingInterval, 30000));
            }
            
            _settingsService.Save();
            DialogResult = true;
            Close();
        }
        catch
        {
            // Ignore save errors
            DialogResult = false;
        }
    }
}
