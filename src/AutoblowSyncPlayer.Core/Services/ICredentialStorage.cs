namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Secure credential storage for device tokens on Windows using DPAPI.
/// </summary>
public interface ICredentialStorage
{
    /// <summary>
    /// Save the device token securely.
    /// </summary>
    void SaveDeviceToken(string token);

    /// <summary>
    /// Retrieve the saved device token. Returns null if not saved.
    /// </summary>
    string? GetDeviceToken();

    /// <summary>
    /// Delete the saved device token.
    /// </summary>
    void DeleteDeviceToken();

    /// <summary>
    /// Whether a device token is saved.
    /// </summary>
    bool HasSavedToken();
}
