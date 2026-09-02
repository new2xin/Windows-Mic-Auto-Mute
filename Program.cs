using System.Text;

namespace LyraAutoMute;

internal static class Program
{
    private const string Usage = """
        LyraAutoMute - Windows録音エンドポイント自動ミュート

        使い方:
          LyraAutoMute.exe --watch  --config devices.json
          LyraAutoMute.exe --mute   --config devices.json
          LyraAutoMute.exe --unmute --config devices.json
          LyraAutoMute.exe --status --config devices.json
          LyraAutoMute.exe --stop
        """;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            var command = GetOption(args, "--watch", "--mute", "--unmute", "--status", "--list", "--stop");
            if (command is null || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h"))
            {
                Console.WriteLine(Usage);
                return command is null ? 2 : 0;
            }

            if (command == "--list")
                return ListDevices();
            if (command == "--stop")
                return StopWatcher();

            var configPath = GetValue(args, "--config") ?? Path.Combine(AppContext.BaseDirectory, "devices.json");
            var config = AppConfig.Load(Path.GetFullPath(configPath));
            var logger = new AppLogger(Path.Combine(AppContext.BaseDirectory, "logs", "automute.log"));

            return command switch
            {
                "--watch" => Watch(config, logger),
                "--mute" => Apply(config, logger, true),
                "--unmute" => Apply(config, logger, false),
                "--status" => Status(config, logger),
                _ => 2
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static int Watch(AppConfig config, AppLogger logger)
    {
        using var singleInstance = new Mutex(true, @"Local\LyraAutoMute.Watcher", out var acquired);
        if (!acquired)
        {
            Console.WriteLine("自動ミュート監視は既に起動しています。");
            return 0;
        }

        using var stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset,
            @"Local\LyraAutoMute.Stop", out _);
        stopEvent.Reset();
        logger.Info($"watch started; interval={config.PollIntervalMs}ms");
        Console.WriteLine("自動ミュート監視中。終了はCtrl+Cです。");
        using var quit = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            quit.Set();
        };

        while (!quit.IsSet)
        {
            try
            {
                ApplyOnce(config, logger, true, print: false);
            }
            catch (Exception ex)
            {
                logger.Error($"watch iteration failed: {ex}");
            }
            var signal = WaitHandle.WaitAny(new WaitHandle[] { quit.WaitHandle, stopEvent }, config.PollIntervalMs);
            if (signal == 1)
                quit.Set();
        }
        logger.Info("watch stopped");
        return 0;
    }

    private static int StopWatcher()
    {
        try
        {
            using var stopEvent = EventWaitHandle.OpenExisting(@"Local\LyraAutoMute.Stop");
            stopEvent.Set();
            Console.WriteLine("自動ミュート監視へ停止を通知しました。");
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            Console.WriteLine("自動ミュート監視は起動していません。");
        }
        return 0;
    }

    private static int Apply(AppConfig config, AppLogger logger, bool mute)
    {
        var matched = ApplyOnce(config, logger, mute, print: true);
        Console.WriteLine(matched == 0 ? "対象デバイスは見つかりませんでした。" : $"{matched}台を処理しました。");
        return 0;
    }

    private static int Status(AppConfig config, AppLogger logger)
    {
        var devices = CoreAudio.GetActiveCaptureDevices();
        var matched = devices.Where(d => config.Targets.Any(t => t.Matches(d))).ToList();
        if (matched.Count == 0)
        {
            Console.WriteLine("対象デバイスは接続・認識されていません。");
            return 0;
        }
        foreach (var device in matched)
        {
            try
            {
                Console.WriteLine($"{(CoreAudio.GetMute(device) ? "MUTED  " : "UNMUTED")}  {device.FriendlyName}");
                Console.WriteLine($"  ID: {device.Id}");
            }
            catch (Exception ex)
            {
                logger.Error($"status failed for {device.FriendlyName}: {ex.Message}");
                Console.WriteLine($"UNKNOWN  {device.FriendlyName}: {ex.Message}");
            }
        }
        return 0;
    }

    private static int ListDevices()
    {
        foreach (var device in CoreAudio.GetActiveCaptureDevices())
            Console.WriteLine($"{device.FriendlyName}\n  {device.Id}");
        return 0;
    }

    private static int ApplyOnce(AppConfig config, AppLogger logger, bool mute, bool print)
    {
        var devices = CoreAudio.GetActiveCaptureDevices();
        var matched = devices.Where(d => config.Targets.Any(t => t.Matches(d))).ToList();
        var count = 0;
        foreach (var device in matched)
        {
            try
            {
                var before = CoreAudio.GetMute(device);
                if (before != mute)
                    CoreAudio.SetMute(device, mute);
                count++;
                if (print)
                    Console.WriteLine($"{device.FriendlyName}: {(before ? "MUTED" : "UNMUTED")} -> {(mute ? "MUTED" : "UNMUTED")}");
                logger.Info($"{device.FriendlyName}: {(before ? "muted" : "unmuted")} -> {(mute ? "muted" : "unmuted")}");
            }
            catch (Exception ex)
            {
                logger.Error($"apply failed for {device.FriendlyName}: {ex.Message}");
                if (print) Console.Error.WriteLine($"{device.FriendlyName}: {ex.Message}");
            }
        }
        return count;
    }

    private static string? GetOption(string[] args, params string[] options) =>
        options.FirstOrDefault(option => args.Contains(option, StringComparer.OrdinalIgnoreCase));

    private static string? GetValue(string[] args, string option)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

internal sealed class AppLogger
{
    private readonly string _path;
    private readonly object _gate = new();

    public AppLogger(string path) => _path = path;

    public void Info(string message) => Write("INFO", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // ログ失敗でミュート処理自体を止めない。
        }
    }
}
