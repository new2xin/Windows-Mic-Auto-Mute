using System.Text.Json;

namespace WindowsMicAutoMute;

public sealed class AppConfig
{
    public int PollIntervalMs { get; set; } = 2000;
    public int StartupTimeoutMs { get; set; } = 30_000;
    public List<DeviceRule> Targets { get; set; } = new();

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"設定ファイルがありません: {path}", path);

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) ?? throw new InvalidDataException("設定ファイルを読み込めませんでした。");

        if (config.Targets.Count == 0)
            throw new InvalidDataException("targets に少なくとも1つのデバイス条件を指定してください。");

        config.PollIntervalMs = Math.Clamp(config.PollIntervalMs, 500, 60_000);
        config.StartupTimeoutMs = Math.Clamp(config.StartupTimeoutMs, 0, 120_000);
        return config;
    }
}

public sealed class DeviceRule
{
    public string NameContains { get; set; } = "";
    public string IdContains { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public bool Matches(AudioCaptureDevice device)
    {
        if (!Enabled)
            return false;

        var nameMatch = string.IsNullOrWhiteSpace(NameContains) ||
            device.FriendlyName.Contains(NameContains, StringComparison.OrdinalIgnoreCase);
        var idMatch = string.IsNullOrWhiteSpace(IdContains) ||
            device.Id.Contains(IdContains, StringComparison.OrdinalIgnoreCase);
        return nameMatch && idMatch;
    }
}
