using System.IO;
using System.Text.Json;
using WardogsTacticalRadio.Models;

namespace WardogsTacticalRadio.Storage;

public static class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WardogsTacticalRadio");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
    }
}
