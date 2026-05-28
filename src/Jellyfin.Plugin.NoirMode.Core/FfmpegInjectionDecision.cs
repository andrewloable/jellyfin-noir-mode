namespace Jellyfin.Plugin.NoirMode.Core;

public sealed record FfmpegInjectionDecision(
    IReadOnlyList<string> Arguments,
    bool Modified,
    bool Applied,
    string Reason,
    string? InputPath);
