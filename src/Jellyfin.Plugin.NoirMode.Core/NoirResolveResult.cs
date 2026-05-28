namespace Jellyfin.Plugin.NoirMode.Core;

public sealed record NoirResolveResult(bool ShouldApply, NoirPreset? Preset, string Reason)
{
    public static NoirResolveResult Disabled(string reason)
    {
        return new NoirResolveResult(false, null, reason);
    }

    public static NoirResolveResult Apply(NoirPreset preset, string reason)
    {
        return new NoirResolveResult(true, preset, reason);
    }
}
