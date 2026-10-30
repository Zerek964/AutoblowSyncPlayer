namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Connection state of the Autoblow device.
/// </summary>
public enum DeviceConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    DeviceOffline,
    UnsupportedDevice,
    LoadingScript,
    Ready,
    Playing,
    Paused,
    ApiError,
    VideoEnded
}
