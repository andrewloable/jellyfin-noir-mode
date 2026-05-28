namespace Jellyfin.Plugin.NoirMode.Core;

public sealed class NoirRuleService
{
    private readonly NoirPresetService _presetService;

    public NoirRuleService(NoirPresetService? presetService = null)
    {
        _presetService = presetService ?? new NoirPresetService();
    }

    public NoirResolveResult Resolve(NoirState state, NoirMediaLookup lookup)
    {
        if (!state.Enabled)
        {
            return NoirResolveResult.Disabled("plugin-disabled");
        }

        var itemId = string.IsNullOrWhiteSpace(lookup.ItemId) ? null : lookup.ItemId;
        var normalizedPath = NoirPath.Normalize(lookup.MediaPath);
        var pathHash = NoirPath.Hash(normalizedPath);

        var itemOverride = state.ItemOverrides.FirstOrDefault(x => Matches(x, itemId, normalizedPath, pathHash));
        if (itemOverride is null)
        {
            return NoirResolveResult.Disabled("no-item-override");
        }

        if (itemOverride.Mode is NoirOverrideMode.Disabled or NoirOverrideMode.Off)
        {
            return NoirResolveResult.Disabled("item-disabled");
        }

        if (!_presetService.TryGet(itemOverride.PresetId, out var preset))
        {
            return NoirResolveResult.Disabled("unknown-preset");
        }

        return NoirResolveResult.Apply(preset, "item-preset");
    }

    private static bool Matches(NoirItemOverride itemOverride, string? itemId, string? normalizedPath, string? pathHash)
    {
        if (!string.IsNullOrWhiteSpace(itemId)
            && string.Equals(itemOverride.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(normalizedPath))
        {
            var overridePath = NoirPath.Normalize(itemOverride.NormalizedMediaPath ?? itemOverride.MediaPath);
            if (string.Equals(overridePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return !string.IsNullOrWhiteSpace(pathHash)
            && string.Equals(itemOverride.MediaPathHash, pathHash, StringComparison.OrdinalIgnoreCase);
    }
}
