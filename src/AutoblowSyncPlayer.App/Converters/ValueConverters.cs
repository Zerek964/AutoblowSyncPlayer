namespace AutoblowSyncPlayer.App.Converters;

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBooleanConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// Converts boolean connected state to a human-readable status string.
/// </summary>
public class ConnectionStatusConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool connected)
            return connected ? "Connected" : "Disconnected";
        return "Unknown";
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts boolean connected state to a connectivity icon emoji.
/// </summary>
public class ConnectivityIconConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool connected)
            return connected ? "🟢" : "🔴";
        return "🟡";
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts boolean connected state to a color for status text.
/// </summary>
public class ConnectionStatusColorConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool connected)
        {
            var brush = new System.Windows.Media.SolidColorBrush();
            if (connected)
                brush.Color = System.Windows.Media.Color.FromRgb(0x10, 0x7C, 0x10); // Green
            else
                brush.Color = System.Windows.Media.Color.FromRgb(0xD1, 0x34, 0x38); // Red

            return brush;
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts a string to Visibility (Collapsed if empty/whitespace, Visible otherwise).
/// </summary>
public class StringToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
            return System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return null;
    }
}

/// <summary>
/// Converts boolean muted state to speaker icon emoji (true=muted shows 🔇, false=unmuted shows 🔊).
/// </summary>
public class BooleanToMuteIconConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool muted)
            return muted ? "🔇" : "🔊";
        return "🔊";
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts boolean to Visibility with reversed logic (true=Collapsed, false=Visible).
/// Used for PasswordBox visibility when ShowToken checkbox toggles.
/// </summary>
public class BooleanToVisibilityReversedConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts boolean sync ready state to readiness icon (true=✓, false=✗).
/// </summary>
public class BooleanToSyncReadyIconConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool ready)
            return ready ? "✓" : "✗";
        return "✗";
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts boolean sync ready state to status text (true="Ready to Sync", false="Not Ready").
/// </summary>
public class BooleanToSyncReadyTextConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool ready)
            return ready ? "Ready to Sync" : "Not Ready";
        return "Not Ready";
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts VideoMode enum to a list of all VideoMode values for ComboBox binding.
/// </summary>
public class VideoModeListConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return System.Enum.GetValues(typeof(AutoblowSyncPlayer.Core.Models.VideoMode));
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}

/// <summary>
/// Converts StereoEye enum to boolean for RadioButton binding.
/// Uses ConverterParameter to specify which StereoEye value to compare against.
/// </summary>
public class StereoEyeToBooleanConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is AutoblowSyncPlayer.Core.Models.StereoEye eye && parameter is string param)
        {
            return eye.ToString() == param;
        }
        return false;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string param && 
            System.Enum.TryParse<AutoblowSyncPlayer.Core.Models.StereoEye>(param, out var eye))
        {
            return eye;
        }
        return AutoblowSyncPlayer.Core.Models.StereoEye.Auto;
    }
}
