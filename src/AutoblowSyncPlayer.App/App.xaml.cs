using AutoblowSyncPlayer.Core.Services;
using AutoblowSyncPlayer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace AutoblowSyncPlayer.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly global::Microsoft.Extensions.Logging.ILogger? _logger;

    public App()
    {
        // Global exception handlers to catch startup crashes
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var services = new ServiceCollection();

        // Logging - use LocalApplicationData (same folder as settings.json)
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoblowSyncPlayer", "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "AutoblowSyncPlayer.log");
        try
        {
            // Test write to verify we can actually write the file
            using var testWrite = new StreamWriter(logPath, append: true);
            testWrite.WriteLine($"[INIT] Log file created at {DateTime.UtcNow:O}");
            testWrite.Flush();
        }
        catch (Exception ex)
        {
            // If file logging fails (e.g., permission issue), at least try to log to console
            Console.WriteLine($"WARNING: Could not write log file at {logPath}: {ex.Message}");
        }

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 10 * 1024 * 1024, shared: true)
            .CreateLogger();

        // Assign to global static so Serilog.Log.ForContext<T>() works in controls
        Log.Logger = serilogLogger;

        serilogLogger.Information("Application starting. Log file: {LogPath}", logPath);
        serilogLogger.Information("Executable location: {ExePath}", System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName);
        serilogLogger.Information("Single-file publish detected: {IsExtracted}", AppContext.BaseDirectory.Contains("Temp"));
        serilogLogger.Information("Base directory: {BaseDir}", AppContext.BaseDirectory);

        services.AddLogging(builder =>
        {
            builder.AddSerilog(serilogLogger);
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core services
        services.AddSingleton<IFunscriptService, FunscriptService>();
        services.AddSingleton<IHeatmapCalculator, HeatmapCalculator>();
        services.AddSingleton<ICredentialStorage, DpapiCredentialStorage>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // Infrastructure services - scoped to application lifetime
        services.AddSingleton<IVideoPlayerService, LibVlcVideoPlayer>();
        services.AddSingleton<IAutoblowDeviceClient, AutoblowDeviceClient>();
        services.AddSingleton<ISyncService, SyncService>();

        // VR3D Infrastructure services
        services.AddSingleton<ICameraController, CameraController>();
        services.AddSingleton<IVideoModeDetector, VideoModeDetector>();
        services.AddSingleton<IVideoRenderer, VideoRenderer>();

        // Register ViewModels
        services.AddSingleton<ViewModels.MainViewModel>();

        _services = services.BuildServiceProvider();

        _logger = _services.GetService<ILogger<App>>();
    }

    public IServiceProvider Services => _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Show loading window during startup
        var loadingWindow = new Views.LoadingWindow();
        loadingWindow.Show();
        loadingWindow.UpdateStatus("Loading settings...");

        // Resolve services
        var settings = _services.GetRequiredService<ISettingsService>();
        var mainViewModel = _services.GetRequiredService<ViewModels.MainViewModel>();
        var videoPlayer = _services.GetRequiredService<IVideoPlayerService>();
        var cameraController = _services.GetRequiredService<ICameraController>();

        // Create and show main window BEFORE loading settings.
        // This ensures the VideoView is wired to the MediaPlayer before any
        // Media is assigned, so LibVLC renders into the WPF control rather
        // than creating its own native window.
        var mainWindow = new Views.MainWindow(mainViewModel, videoPlayer, settings, cameraController);
        mainWindow.Show();

        // Now safe to close loading window
        loadingWindow.Close();

        // Set main window
        MainWindow = mainWindow;

        // Load settings asynchronously. OnStartup runs on the UI thread,
        // so async continuations will marshal back to the UI thread,
        // ensuring LibVLC Media assignment happens on the correct thread.
        _ = mainViewModel.LoadSettings(settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Attempt to stop the device when the application closes (safety requirement)
        try
        {
            var deviceClient = _services.GetRequiredService<IAutoblowDeviceClient>();
            if (deviceClient.IsConnected)
            {
                _logger?.LogInformation("Application exiting - stopping device script...");
                // Run on thread pool to avoid SynchronizationContext deadlock on UI thread
                Task.Run(async () => { await deviceClient.StopScriptAsync(); }).Wait();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to stop device on exit: {Message}", ex.Message);
        }

        // Save settings on application exit — all synchronous, guaranteed to run before shutdown
        var settings = _services.GetRequiredService<ISettingsService>();
        var mainViewModel = _services.GetRequiredService<ViewModels.MainViewModel>();
        var credentialStorage = _services.GetRequiredService<ICredentialStorage>();

        mainViewModel.SaveSettings(settings);

        // Ensure device token is persisted to secure storage
        if (!string.IsNullOrEmpty(mainViewModel.DeviceToken))
        {
            credentialStorage.SaveDeviceToken(mainViewModel.DeviceToken);
        }

        settings.Save();

        base.OnExit(e);
    }

    private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString());
        MessageBox.Show($"Fatal application error: {ex}\n\nThe application will now close.",
            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred: {e.Exception}\n\nThe application will continue running.",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
