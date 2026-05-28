# ADR 0001: Force Transcode And Core Hook Feasibility

## Status

Accepted for MVP 1.

## Decision

MVP 1 does not claim plugin-only force-transcode support. Noir Mode applies only when Jellyfin invokes FFmpeg for video transcoding and the wrapper sees a safe command to modify.

Videos are disabled by default. A per-video preset override is required before the wrapper attempts any filter injection.

## Rationale

Jellyfin's public plugin model supports plugins, configuration pages, services, and API endpoints, but the current public plugin surface does not expose a stable pre-playback hook that can rewrite all client playback decisions or append filters inside Jellyfin's native FFmpeg command builder.

Direct Play and Direct Stream do not run video filters. A wrapper can only modify commands that Jellyfin actually sends to FFmpeg. If Jellyfin chooses stream copy or a complex/hardware graph, MVP 1 passes through unchanged and logs the reason.

## MVP Contract

- Per-video Noir Mode setting defaults to disabled.
- Per-video preset override enables Noir Mode for that item.
- The wrapper injects filters only for safe software video transcode commands.
- Stream copy, `-filter_complex`, and unsupported hardware chains pass through unchanged.
- Diagnostics explain every apply/skip decision.

## Future Core Hook

A cleaner long-term Jellyfin change would add an extension point similar to:

```csharp
public interface ITranscodingFilterProvider
{
    bool AppliesTo(TranscodingFilterContext context);
    IReadOnlyList<string> GetVideoFilters(TranscodingFilterContext context);
}
```

That would let plugins participate before final FFmpeg argument generation and would remove the need for wrapper argument rewriting.
