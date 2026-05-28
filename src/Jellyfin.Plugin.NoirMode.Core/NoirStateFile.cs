using System.Text.Json;

namespace Jellyfin.Plugin.NoirMode.Core;

public static class NoirStateFile
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static NoirState Read(string path)
    {
        if (!File.Exists(path))
        {
            return new NoirState { Enabled = false };
        }

        using var stream = File.OpenRead(path);
        var state = JsonSerializer.Deserialize<NoirState>(stream, JsonOptions);
        return state ?? new NoirState { Enabled = false };
    }

    public static void WriteAtomic(string path, NoirState state)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        state.GeneratedAt = DateTimeOffset.UtcNow;

        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, state, JsonOptions);
        }

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }
}
