using AutoblowSyncPlayer.App.ViewModels;
using AutoblowSyncPlayer.Core.Services;
using LibVLCSharp.Shared;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoblowSyncPlayer.App.Views;

public partial class MainWindow : Window
{
    private readonly IVideoPlayerService _videoPlayer;
    private readonly ISettingsService _settingsService;
    private readonly MainViewModel _viewModel;
    private readonly ICameraController _cameraController;

    // Mouse state for camera control
    private bool _isMouseDragging = false;
    private Point _lastMousePosition;

    public MainWindow(MainViewModel viewModel, IVideoPlayerService videoPlayer, ISettingsService settingsService, ICameraController cameraController)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _videoPlayer = videoPlayer;
        _settingsService = settingsService;
        _cameraController = cameraController;

        // Wire the VideoView to the MediaPlayer immediately in the constructor.
        // LibVLC MediaPlayer must be created on the same thread (UI thread) that
        // hosts the VideoView, otherwise it opens its own native window.
        _videoPlayer.Initialize();
        WireMediaPlayer();

        // Restore window size from settings
        if (_settingsService.RememberWindowSize)
        {
            Width = Math.Max(800, _settingsService.WindowWidth);
            Height = Math.Max(600, _settingsService.WindowHeight);
        }

        // Sync PasswordBox when DeviceToken changes externally (e.g., loaded from settings)
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Wire heatmap click-to-seek
        HeatmapControl.SeekRequested += OnHeatmapSeekRequested;
    }

    /// <summary>
    /// Wires the VideoView to the MediaPlayer if not already wired.
    /// </summary>
    private void WireMediaPlayer()
    {
        if (_videoPlayer.MediaPlayerHandle is LibVLCSharp.Shared.MediaPlayer mediaPlayer && videoView.MediaPlayer == null)
        {
            videoView.MediaPlayer = mediaPlayer;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
    }

    /// <summary>
    /// Handles click-to-seek from the heatmap control.
    /// </summary>
    private async void OnHeatmapSeekRequested(object? sender, double percentage)
    {
        if (!_viewModel.IsVideoLoaded) return;

        try
        {
            await _viewModel.SeekToPositionCommand.ExecuteAsync(percentage);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Seek error: {ex.Message}";
        }
    }

    // Drag-and-drop handlers for MP4 files
    private async void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            foreach (var file in files)
            {
                if (file.ToLowerInvariant().EndsWith(".mp4"))
                {
                    await _viewModel.LoadVideoAsync(file);
                    break; // Only load first MP4
                }
            }
        }
        e.Handled = true;
    }

    // Mouse handlers for VR3D camera control
    private async void OnVideoViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_viewModel.CameraModeEnabled) return;

        _isMouseDragging = true;
        if (videoView.IsMouseOver)
        {
            videoView.CaptureMouse();
        }
        _lastMousePosition = e.GetPosition(this);
        e.Handled = true;
    }

    private async void OnVideoViewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_viewModel.CameraModeEnabled || !_isMouseDragging) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPosition = e.GetPosition(this);
        var deltaX = currentPosition.X - _lastMousePosition.X;
        var deltaY = currentPosition.Y - _lastMousePosition.Y;
        _lastMousePosition = currentPosition;

        await _viewModel.HandleMouseDeltaAsync((float)deltaX, (float)deltaY);
        e.Handled = true;
    }

    private void OnVideoViewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMouseDragging && videoView.IsMouseOver)
        {
            videoView.ReleaseMouseCapture();
        }
        _isMouseDragging = false;
        e.Handled = true;
    }

    private async void OnVideoViewMouseWheelChanged(object sender, MouseWheelEventArgs e)
    {
        if (!_viewModel.CameraModeEnabled) return;

        var delta = (float)e.Delta / 120.0f; // Normalize to ~1 per notch
        await _viewModel.HandleWheelDeltaAsync(delta);
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        HeatmapControl.SeekRequested -= OnHeatmapSeekRequested;

        // Persist window size if enabled
        if (_settingsService.RememberWindowSize)
        {
            _settingsService.WindowWidth = (int)RestoreBounds.Width;
            _settingsService.WindowHeight = (int)RestoreBounds.Height;
            _settingsService.Save();
        }

        base.OnClosed(e);
    }
}
