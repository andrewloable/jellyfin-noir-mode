using Jellyfin.Plugin.NoirMode.Configuration;
using Jellyfin.Plugin.NoirMode.Core;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.NoirMode.Services;

public sealed class NoirPluginStateService
{
    private readonly IApplicationPaths _applicationPaths;

    public NoirPluginStateService(IApplicationPaths applicationPaths)
    {
        _applicationPaths = applicationPaths;
    }

    public string StateFilePath => Path.Combine(_applicationPaths.PluginConfigurationsPath, "jellyfin-noir-mode-state.json");

    public NoirState BuildState(PluginConfiguration configuration)
    {
        return new NoirState
        {
            Enabled = configuration.Enabled,
            ItemOverrides = configuration.ItemOverrides
                .Select(Normalize)
                .ToList()
        };
    }

    public void Export(PluginConfiguration configuration)
    {
        NoirStateFile.WriteAtomic(StateFilePath, BuildState(configuration));
    }

    private static NoirItemOverride Normalize(NoirItemOverride itemOverride)
    {
        var normalizedPath = NoirPath.Normalize(itemOverride.MediaPath ?? itemOverride.NormalizedMediaPath);
        return new NoirItemOverride
        {
            ItemId = itemOverride.ItemId,
            MediaPath = itemOverride.MediaPath,
            NormalizedMediaPath = normalizedPath,
            MediaPathHash = NoirPath.Hash(normalizedPath),
            Mode = itemOverride.Mode,
            PresetId = itemOverride.PresetId
        };
    }
}
