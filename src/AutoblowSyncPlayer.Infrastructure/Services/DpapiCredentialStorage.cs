using AutoblowSyncPlayer.Core.Services;
using System.Security.Cryptography;
using System.Text;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Stores device tokens using Windows DPAPI (Data Protection API).
/// Only available on Windows.
/// </summary>
public class DpapiCredentialStorage : ICredentialStorage
{
    private const string DpapiEntropy = "AutoblowSyncPlayer_DeviceToken_v1";

    public void SaveDeviceToken(string token)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(token);
        var encryptedBytes = ProtectedData.Protect(
            plainTextBytes,
            Encoding.UTF8.GetBytes(DpapiEntropy),
            DataProtectionScope.CurrentUser);

        var settingsPath = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, Convert.ToBase64String(encryptedBytes));
    }

    public string? GetDeviceToken()
    {
        var settingsPath = GetSettingsPath();

        if (!File.Exists(settingsPath))
            return null;

        try
        {
            var base64 = File.ReadAllText(settingsPath);
            var encryptedBytes = Convert.FromBase64String(base64);
            var plainTextBytes = ProtectedData.Unprotect(
                encryptedBytes,
                Encoding.UTF8.GetBytes(DpapiEntropy),
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainTextBytes);
        }
        catch
        {
            return null;
        }
    }

    public void DeleteDeviceToken()
    {
        var settingsPath = GetSettingsPath();
        if (File.Exists(settingsPath))
            File.Delete(settingsPath);
    }

    public bool HasSavedToken()
    {
        return File.Exists(GetSettingsPath());
    }

    private static string GetSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "AutoblowSyncPlayer", "device-credentials.dat");
    }
}
