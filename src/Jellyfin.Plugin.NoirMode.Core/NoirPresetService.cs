namespace Jellyfin.Plugin.NoirMode.Core;

public sealed class NoirPresetService
{
    private static readonly NoirPreset[] BuiltInPresets =
    [
        new("classic-bw", "Classic B&W", "hue=s=0"),
        new("film-noir", "Film Noir", "hue=s=0,eq=contrast=1.35:brightness=-0.03"),
        new("high-contrast", "High Contrast Noir", "hue=s=0,eq=contrast=1.6:brightness=-0.06"),
        new("vintage-noir", "Vintage Noir", "hue=s=0,eq=contrast=1.25:brightness=-0.02,noise=alls=12:allf=t+u"),
        new("soft-noir", "Soft Noir", "hue=s=0,eq=contrast=1.15:brightness=0.01")
    ];

    private readonly IReadOnlyDictionary<string, NoirPreset> _presets;

    public NoirPresetService()
        : this(BuiltInPresets)
    {
    }

    public NoirPresetService(IEnumerable<NoirPreset> presets)
    {
        _presets = presets.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<NoirPreset> GetAll()
    {
        return _presets.Values.OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool TryGet(string? presetId, out NoirPreset preset)
    {
        if (!string.IsNullOrWhiteSpace(presetId) && _presets.TryGetValue(presetId, out var found))
        {
            preset = found;
            return true;
        }

        preset = new NoirPreset(string.Empty, string.Empty, string.Empty);
        return false;
    }

    public NoirPreset GetRequired(string presetId)
    {
        if (TryGet(presetId, out var preset))
        {
            return preset;
        }

        throw new ArgumentException($"Unknown Noir Mode preset '{presetId}'.", nameof(presetId));
    }
}
