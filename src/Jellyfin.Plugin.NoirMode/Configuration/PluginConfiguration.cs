using Jellyfin.Plugin.NoirMode.Core;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NoirMode.Configuration;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; }

    public bool AllowCustomFilters { get; set; }

    public bool ForceTranscodeNoticeShown { get; set; }

    public string? RealFFmpegPath { get; set; }

    public string? WrapperPath { get; set; }

    public List<NoirItemOverride> ItemOverrides { get; set; } = [];
}
