using System;
using System.IO;
using System.Text.Json;

namespace ScarlettMonitorOSD;

public class AppSettings
{
    public double BackgroundOpacity { get; set; } = 1.0;
    public double DisplayDurationSeconds { get; set; } = 2.0;
    public double VolumeStepDb { get; set; } = 1.0;
    public double MaxGainLimit { get; set; } = 0.0;
    public bool AutoLaunchFc2 { get; set; } = true;
    public bool KillFc2OnExit { get; set; } = false;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string GetConfigPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            string path = GetConfigPath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    settings.BackgroundOpacity = Math.Clamp(settings.BackgroundOpacity, 0.0, 1.0);
                    settings.DisplayDurationSeconds = Math.Clamp(settings.DisplayDurationSeconds, 0.5, 10.0);
                    settings.VolumeStepDb = Math.Clamp(settings.VolumeStepDb, 0.5, 5.0);
                    settings.MaxGainLimit = Math.Clamp(settings.MaxGainLimit, -30.0, 6.0);
                    return settings;
                }
            }
        }
        catch {}
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string path = GetConfigPath();
            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch {}
    }
}
