using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Serilog;
using ILogger = Serilog.ILogger;

namespace AutoblowSyncPlayer.App.Controls;

/// <summary>
/// Interaction logic for FunscriptHeatmap.xaml
/// </summary>
public partial class FunscriptHeatmap : UserControl
{
    private static readonly ILogger Logger = Log.ForContext<FunscriptHeatmap>();
    private WriteableBitmap? _bitmap;
    private bool _isRendering;
    private System.Windows.Threading.DispatcherTimer? _hoverTimer;
    private ToolTip? _hoverTooltip;

    /// <summary>
    /// Gets or sets the heatmap activity data.
    /// </summary>
    public static readonly DependencyProperty ActivitiesProperty =
        DependencyProperty.Register(
            nameof(Activities),
            typeof(IReadOnlyList<AutoblowSyncPlayer.Core.Models.HeatmapActivity>),
            typeof(FunscriptHeatmap),
            new PropertyMetadata(null, OnActivitiesChanged));

    /// <summary>
    /// Gets or sets the current video progress (0-100).
    /// </summary>
    public static readonly DependencyProperty VideoProgressProperty =
        DependencyProperty.Register(
            nameof(VideoProgress),
            typeof(double),
            typeof(FunscriptHeatmap),
            new PropertyMetadata(0.0, OnVideoProgressChanged));

    /// <summary>
    /// Gets or sets whether a script is loaded.
    /// </summary>
    public static readonly DependencyProperty HasScriptProperty =
        DependencyProperty.Register(
            nameof(HasScript),
            typeof(bool),
            typeof(FunscriptHeatmap),
            new PropertyMetadata(false, OnHasScriptChanged));

    /// <summary>
    /// Gets or sets whether the script exceeds the video duration.
    /// </summary>
    public static readonly DependencyProperty ScriptExceedsVideoProperty =
        DependencyProperty.Register(
            nameof(ScriptExceedsVideo),
            typeof(bool),
            typeof(FunscriptHeatmap),
            new PropertyMetadata(false, OnScriptExceedsVideoChanged));

    /// <summary>
    /// Gets or sets the heatmap activity data.
    /// </summary>
    public IReadOnlyList<AutoblowSyncPlayer.Core.Models.HeatmapActivity>? Activities
    {
        get => (IReadOnlyList<AutoblowSyncPlayer.Core.Models.HeatmapActivity>?)GetValue(ActivitiesProperty);
        set => SetValue(ActivitiesProperty, value);
    }

    /// <summary>
    /// Gets or sets the current video progress (0-100).
    /// </summary>
    public double VideoProgress
    {
        get => (double)GetValue(VideoProgressProperty);
        set => SetValue(VideoProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets whether a script is loaded.
    /// </summary>
    public bool HasScript
    {
        get => (bool)GetValue(HasScriptProperty);
        set => SetValue(HasScriptProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the script exceeds the video duration.
    /// </summary>
    public bool ScriptExceedsVideo
    {
        get => (bool)GetValue(ScriptExceedsVideoProperty);
        set => SetValue(ScriptExceedsVideoProperty, value);
    }

    /// <summary>
    /// Raised when user clicks on the heatmap to seek to a position.
    /// </summary>
    public event EventHandler<double>? SeekRequested;

    public FunscriptHeatmap()
    {
        Logger.Information("[HEATMAP] Constructor called - instance created");
        InitializeComponent();

        // Diagnose DataContext
        Logger.Information("[HEATMAP] After InitializeComponent - DataContext: {DataContextType}",
            DataContext?.GetType().Name ?? "null");

        DataContextChanged += (s, e) =>
        {
            Logger.Information("[HEATMAP] DataContextChanged - New: {DataContextType}, Old: {OldDataContextType}",
                DataContext?.GetType().Name ?? "null",
                e.OldValue?.GetType().Name ?? "null");
        };

        Loaded += (s, e) =>
        {
            Logger.Information("[HEATMAP] Loaded event fired - DataContext: {DataContextType}, Parent: {ParentType}",
                DataContext?.GetType().Name ?? "null",
                Parent?.GetType().Name ?? "null");
            
            // Check binding expressions
            var bindingExpr = GetBindingExpression(ActivitiesProperty);
            Logger.Information("[HEATMAP] Activities binding expression: {BindingExprType}",
                bindingExpr?.GetType().Name ?? "null");
            
            var hasScriptBindingExpr = GetBindingExpression(HasScriptProperty);
            Logger.Information("[HEATMAP] HasScript binding expression: {BindingExprType}",
                hasScriptBindingExpr?.GetType().Name ?? "null");
        };

        // Setup hover tooltip timer with 300ms delay
        _hoverTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300),
            IsEnabled = false
        };
        _hoverTimer.Tick += (s, e) =>
        {
            _hoverTimer!.Stop();
            ShowHoverTooltip();
        };

        // Hook up mouse events for hover and click-to-seek
        HeatmapImage.MouseMove += OnHeatmapMouseMove;
        HeatmapImage.MouseLeave += OnHeatmapMouseLeave;
        HeatmapImage.MouseDown += OnHeatmapMouseDown;
    }

    private static void OnActivitiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunscriptHeatmap control)
        {
            control.RenderHeatmap();
        }
    }

    private static void OnVideoProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunscriptHeatmap control)
        {
            control.UpdatePlaybackMarker();
        }
    }

    private static void OnHasScriptChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunscriptHeatmap control)
        {
            bool hasScript = (bool)e.NewValue;
            control.PlaceholderText.Visibility = hasScript ? Visibility.Collapsed : Visibility.Visible;
            control.HeatmapGrid.Visibility = hasScript ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnScriptExceedsVideoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FunscriptHeatmap control)
        {
            bool exceeds = (bool)e.NewValue;
            control.DurationWarningBorder.Visibility = exceeds ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void RenderHeatmap()
    {
        if (_isRendering)
        {
            return;
        }

        // Reset previous bitmap to free memory
        _bitmap?.Freeze();
        _bitmap = null;

        if (Activities == null || Activities.Count == 0)
        {
            HeatmapImage.Source = null;
            return;
        }

        _isRendering = true;

        // Capture all UI-thread-bound values BEFORE spawning the background task.
        // ActualWidth/ActualHeight and DependencyProperty accessors throw
        // DispatcherOperationException when accessed from a background thread.
        var pixelWidth = (int)ActualWidth;
        var pixelHeight = (int)ActualHeight;
        var activities = Activities;

        // Render on background thread to avoid blocking UI
        Task.Run(() =>
        {
            try
            {
                // Fallback if size not yet available
                if (pixelWidth <= 0) pixelWidth = 800;
                if (pixelHeight <= 0) pixelHeight = 40;

                int bucketCount = activities.Count;

                // Build pixel data in memory, then write all at once via WritePixels
                int bytesPerPixel = 4; // Bgr32 = 4 bytes
                int stride = pixelWidth * bytesPerPixel;
                byte[] pixels = new byte[stride * pixelHeight];

                for (int y = 0; y < pixelHeight; y++)
                {
                    for (int x = 0; x < pixelWidth; x++)
                    {
                        // Map pixel to bucket
                        double ratio = (double)x / pixelWidth;
                        int bucketIndex = (int)(ratio * bucketCount);
                        bucketIndex = Math.Clamp(bucketIndex, 0, bucketCount - 1);

                        double activityLevel = activities[bucketIndex].ActivityLevel;
                        Color color = ActivityToColor(activityLevel);

                        // Bgr32: Blue, Green, Red, Alpha (little-endian)
                        int index = (y * stride + x * bytesPerPixel);
                        pixels[index + 0] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = 255;
                    }
                }

                var bitmap = new WriteableBitmap(
                    pixelWidth, pixelHeight,
                    96, 96,
                    PixelFormats.Bgr32, null);

                bitmap.WritePixels(
                    new Int32Rect(0, 0, pixelWidth, pixelHeight),
                    pixels, stride, 0);

                // Freeze on the background thread (where bitmap was created) before crossing threads
                bitmap.Freeze();

                // Update UI on dispatcher thread — use BeginInvoke to avoid blocking deadlock
                // when the UI thread is occupied with seek/stop operations
                Dispatcher.BeginInvoke(() =>
                {
                    _bitmap = bitmap;
                    HeatmapImage.Source = bitmap;
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[HEATMAP] RenderHeatmap: exception during rendering");
            }
            finally
            {
                _isRendering = false;
            }
        });
    }

    private static Color ActivityToColor(double activityLevel)
    {
        // Color gradient:
        // 0.0     → dark gray
        // 0-0.25  → blue/teal
        // 0.25-0.5 → yellow
        // 0.5-0.75 → orange
        // 0.75-1.0 → red

        if (activityLevel <= 0.01)
        {
            // Near-zero activity: dark gray background
            return Color.FromArgb(255, 42, 42, 42);
        }

        if (activityLevel <= 0.25)
        {
            // Blue to teal
            double t = activityLevel / 0.25;
            return Color.FromArgb(255,
                (byte)Lerp(30, 0, t),
                (byte)Lerp(60, 150, t),
                (byte)Lerp(120, 200, t));
        }

        if (activityLevel <= 0.5)
        {
            // Teal to yellow
            double tealT = (activityLevel - 0.25) / 0.25;
            return Color.FromArgb(255,
                (byte)Lerp(0, 255, tealT),
                (byte)Lerp(150, 200, tealT),
                (byte)Lerp(200, 0, tealT));
        }

        if (activityLevel <= 0.75)
        {
            // Yellow to orange
            double yellowT = (activityLevel - 0.5) / 0.25;
            return Color.FromArgb(255,
                (byte)Lerp(255, 255, yellowT),
                (byte)Lerp(200, 100, yellowT),
                (byte)Lerp(0, 0, yellowT));
        }

        // Orange to red
        double redT = (activityLevel - 0.75) / 0.25;
        return Color.FromArgb(255,
            (byte)Lerp(255, 255, redT),
            (byte)Lerp(100, 30, redT),
            (byte)Lerp(0, 0, redT));
    }

    private static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * t;
    }

    private void UpdatePlaybackMarker()
    {
        if (HeatmapImage.ActualWidth <= 0)
            return;

        double ratio = Math.Clamp(VideoProgress / 100.0, 0.0, 1.0);

        // Position the marker line
        PlaybackMarker.X1 = ratio * HeatmapImage.ActualWidth;
        PlaybackMarker.X2 = ratio * HeatmapImage.ActualWidth;

        // Position the triangle indicator at the top
        Polygon? triangle = MarkerCanvas.Children.OfType<Polygon>().FirstOrDefault();
        if (triangle != null)
        {
            Canvas.SetLeft(triangle, ratio * HeatmapImage.ActualWidth - 6);
        }
    }

    private void OnHeatmapMouseMove(object sender, MouseEventArgs e)
    {
        if (Activities == null || Activities.Count == 0)
            return;

        _hoverTimer?.Start();
    }

    private void OnHeatmapMouseLeave(object sender, MouseEventArgs e)
    {
        _hoverTimer?.Stop();
        HideHoverTooltip();
    }

    private void OnHeatmapMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Activities == null || Activities.Count == 0)
            return;

        var point = e.GetPosition(HeatmapImage);
        double ratio = Math.Clamp(point.X / HeatmapImage.ActualWidth, 0.0, 1.0);
        double seekPercent = ratio * 100.0;

        SeekRequested?.Invoke(this, seekPercent);
    }

    private void ShowHoverTooltip()
    {
        if (Activities == null || Activities.Count == 0)
            return;

        double mouseX = Mouse.GetPosition(HeatmapImage).X;
        if (mouseX < 0 || HeatmapImage.ActualWidth <= 0)
            return;

        double ratio = Math.Clamp(mouseX / HeatmapImage.ActualWidth, 0.0, 1.0);
        double positionMs = ratio * (Activities[^1].EndMs);

        int bucketIndex = (int)(ratio * Activities.Count);
        bucketIndex = Math.Clamp(bucketIndex, 0, Activities.Count - 1);
        double activity = Activities[bucketIndex].ActivityLevel;

        string timeString = TimeSpan.FromMilliseconds(positionMs).ToString(@"hh\:mm\:ss");
        string tooltipText = $"Time: {timeString}\nActivity: {activity:P1}";

        HideHoverTooltip();

        _hoverTooltip = new ToolTip
        {
            Content = tooltipText,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
            HorizontalOffset = 10,
            VerticalOffset = -10
        };
        _hoverTooltip.IsOpen = true;
    }

    private void HideHoverTooltip()
    {
        if (_hoverTooltip != null)
        {
            _hoverTooltip.IsOpen = false;
            _hoverTooltip = null;
        }
    }

    /// <summary>
    /// Re-render the heatmap when the control is resized.
    /// </summary>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        // Re-render if size changed significantly
        if (Activities != null && Activities.Count > 0)
        {
            var newWidth = sizeInfo.NewSize.Width;
            var oldWidth = sizeInfo.PreviousSize.Width;

            if (newWidth > 0 && oldWidth > 0 && Math.Abs(newWidth - oldWidth) > 10)
            {
                RenderHeatmap();
            }
        }
    }
}
