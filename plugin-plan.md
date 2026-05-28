# Jellyfin Noir Mode Plugin Plan

## Goal

Build a GPLv3 Jellyfin plugin that can apply selectable noir-style video looks during playback. The effect must be applied server-side with FFmpeg so the resulting stream works in any Jellyfin client.

The practical interpretation of "any Jellyfin client" is:

- The noir effect is controlled by server-side plugin state.
- Playback clients receive a normal Jellyfin stream.
- Client-specific player UI is optional and cannot be the only way to enable the feature.

## Requirements

- Provide a plugin enable/disable setting for wrapper and admin features.
- Provide multiple noir modes.
- Allow admins to set Noir Mode per video.
- Default every video's Noir Mode setting to disabled.
- Leave videos in normal color unless that specific video has a Noir Mode preset override.
- Allow mode selection without modifying media files.
- Apply the selected mode during Jellyfin transcoding.
- Work with any Jellyfin client that can play Jellyfin transcoded output.
- Package and license the plugin under GPLv3.

## Current Technical Constraint

Jellyfin plugins can add configuration pages, API endpoints, services, scheduled tasks, and server integrations. Current public Jellyfin plugin documentation and existing plugin examples do not expose a stable plugin extension point for appending arbitrary FFmpeg arguments to Jellyfin's native transcoding command.

Because of that, there are two viable implementation paths:

1. FFmpeg wrapper plugin architecture: recommended for this project.
2. Jellyfin core patch adding a transcoding filter extension point: cleaner long term, but requires maintaining or upstreaming Jellyfin changes.

The first version should use the FFmpeg wrapper approach because it can work without a Jellyfin fork.

## Autoplan Review: Implementation Issues And Resolutions

### Blocker 1: No Stable Jellyfin Transcode Argument Hook

Jellyfin's public plugin surface does not currently provide a stable hook for appending video filters to its native FFmpeg command generation. A normal plugin should not promise direct insertion into Jellyfin's internal transcoding pipeline.

Resolution: implement Noir Mode through a managed FFmpeg wrapper configured as Jellyfin's FFmpeg path. Keep a long-term Jellyfin core extension point as a separate upstream/fork track.

### Blocker 2: Direct Play Bypasses FFmpeg

Noir Mode cannot affect Direct Play or Direct Stream because no video filter runs when Jellyfin sends the original file or remuxed stream without video transcoding.

Resolution: document the guarantee precisely: Noir Mode works on any Jellyfin client when that playback path uses server-side video transcoding. Add admin diagnostics and setup guidance for disabling Direct Play where required. Treat true all-client, all-playback forcing as a research task because it may require Jellyfin core changes or client-specific settings.

### Blocker 3: Client UI Cannot Be Universal

Jellyfin Web can potentially expose a player toggle, but native clients do not automatically load plugin UI. A player-side preset selector cannot be the universal control plane.

Resolution: make per-video server-side item overrides the source of truth. Clients without a picker still work because the saved per-video setting is applied by the server-side wrapper. Videos without an override play normally. The Jellyfin Web toggle remains optional.

### Blocker 4: Wrapper Scripts Are Fragile On Windows

Configuring Jellyfin's FFmpeg path to a `.ps1` script is likely to fail or require shell-specific behavior. Argument quoting is also riskier across Windows, Linux, and Docker.

Resolution: build a small native wrapper executable as the primary wrapper. It should be a .NET console app published per runtime, for example `ffmpeg-noir-wrapper.exe` on Windows and `ffmpeg-noir-wrapper` on Linux. Shell scripts may remain only as developer helpers.

### Blocker 5: Session/User Resolution Is Limited

The wrapper receives FFmpeg arguments, not a first-class Jellyfin playback context. Input path may identify the media item, but not always the user, client, or session.

Resolution: MVP behavior should depend on per-video item rules only. Per-user and per-session rules are optional future work unless a reliable correlation mechanism is proven.

### Blocker 6: Filter Graphs And Hardware Acceleration Are Easy To Break

Subtitle burn-in and hardware acceleration can produce complex FFmpeg commands. Blindly appending `-vf` can break playback.

Resolution: MVP supports only simple software-transcode filter injection. Commands containing `-filter_complex` or unsupported hardware filter chains should pass through unchanged with structured logs. Add explicit tasks for later compatibility expansion.

## Architecture

### Components

- `Jellyfin.Plugin.NoirMode`
  - Standard Jellyfin plugin assembly.
  - Owns configuration, preset definitions, admin UI, and plugin API endpoints.

- `PluginConfiguration`
  - Stores plugin settings, preset definitions, and per-video item overrides.

- `NoirPresetService`
  - Validates and resolves built-in noir presets.
  - Prevents unsafe arbitrary FFmpeg filter injection.

- `NoirRuleService`
  - Resolves the active noir mode for a media item.
  - Applies per-video item overrides and otherwise returns disabled.

- `FFmpegWrapperService`
  - Installs and manages the native FFmpeg wrapper executable.
  - Tracks the real FFmpeg binary path.
  - Reports wrapper install/status information.

- FFmpeg wrapper
  - Replaces Jellyfin's configured FFmpeg path.
  - Receives the normal Jellyfin FFmpeg arguments.
  - Resolves whether noir mode should apply.
  - Injects a validated video filter into the command.
  - Calls the real FFmpeg binary.

- Optional Jellyfin Web integration
  - Adds a player-side toggle and preset menu for Jellyfin Web only.
  - Writes temporary or item-level plugin state through the plugin API.
  - This is an enhancement, not the universal compatibility mechanism.

## Plugin Scaffold

Start from the official Jellyfin plugin template:

- Repository: `https://github.com/jellyfin/jellyfin-plugin-template`
- Use a plugin ID such as `Jellyfin.Plugin.NoirMode`.
- Target the Jellyfin server SDK version supported by the intended Jellyfin release line.
- Keep the repository GPLv3.

Expected project shape:

```text
Jellyfin.Plugin.NoirMode/
  Configuration/
    PluginConfiguration.cs
  Controllers/
    NoirModeController.cs
  Pages/
    configPage.html
    configPage.js
  Services/
    NoirPresetService.cs
    NoirRuleService.cs
    FFmpegWrapperService.cs
  Wrapper/
    Jellyfin.Plugin.NoirMode.Wrapper/
      Program.cs
      FFmpegArgumentParser.cs
      NoirFilterInjector.cs
  Plugin.cs
  PluginServiceRegistrator.cs
manifest.json
build.yaml
```

## Configuration Model

`PluginConfiguration` should include:

```csharp
public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = false;
    public bool AllowCustomFilters { get; set; } = false;
    public bool ForceTranscodeNoticeShown { get; set; } = false;
    public string? RealFFmpegPath { get; set; }
    public string? WrapperPath { get; set; }
    public List<NoirPreset> Presets { get; set; } = [];
    public List<NoirItemOverride> ItemOverrides { get; set; } = [];
}
```

Rules should support:

- Per-video item override.
- Optional temporary Jellyfin Web selection that writes or previews the current video's item override.

Recommended rule precedence:

1. Temporary Jellyfin Web selection for the current video, if implemented.
2. Per-video item override.
3. Disabled: play the video normally.

The plugin should document that per-user and per-session behavior is out of scope for the MVP because the FFmpeg process arguments do not always carry enough stable user/session identity.

## Per-Video Noir Setting

Noir Mode should be configured per video once the plugin is installed. This should be implemented as a server-side item override stored in the plugin configuration or plugin data store, keyed by Jellyfin item ID and, where useful for wrapper lookup, normalized media path.

Recommended per-video states:

- Disabled: default state; no Noir Mode for this video.
- Off: explicitly disable Noir Mode for this video.
- Specific preset: always use a selected noir preset for this video.

The per-video setting should be available from the plugin admin UI as a searchable "Item overrides" management view. If Jellyfin exposes a safe way to add item-level actions or metadata page controls, add a convenient "Noir Mode" selector from the item's detail/admin context as a later enhancement.

The wrapper should apply Noir Mode only when a per-video override selects a preset. A missing override is treated as disabled. This makes item-specific choices work for any client as long as that playback is transcoded, while all other videos remain unchanged.

## Noir Presets

Start with an allowlisted set of built-in presets:

| ID | Label | FFmpeg filter |
| --- | --- | --- |
| `classic-bw` | Classic B&W | `hue=s=0` |
| `film-noir` | Film Noir | `hue=s=0,eq=contrast=1.35:brightness=-0.03` |
| `high-contrast` | High Contrast Noir | `hue=s=0,eq=contrast=1.6:brightness=-0.06` |
| `vintage-noir` | Vintage Noir | `hue=s=0,eq=contrast=1.25:brightness=-0.02,noise=alls=12:allf=t+u` |
| `soft-noir` | Soft Noir | `hue=s=0,eq=contrast=1.15:brightness=0.01` |

Custom filters should be disabled by default. If custom filters are later enabled, validate them with an allowlist of accepted FFmpeg filter names and characters.

## FFmpeg Wrapper Design

### Installation

The admin configuration page should:

1. Detect Jellyfin's current FFmpeg path.
2. Save that path as `RealFFmpegPath`.
3. Install or point to the native wrapper executable for the host OS.
4. Ask the admin to set Jellyfin's FFmpeg path to the wrapper path, or perform the update if a supported server setting API is available.
5. Provide a status check showing whether Jellyfin is using the wrapper.

The wrapper must never overwrite the real FFmpeg binary.

The primary wrapper implementation should be a native executable, not a PowerShell or shell script. Wrapper scripts may be kept for local development only.

### Runtime Flow

1. Jellyfin starts a transcode and invokes the configured FFmpeg path.
2. The wrapper receives the full original FFmpeg argument list.
3. The wrapper identifies the input media path from `-i`.
4. The wrapper reads a plugin-maintained state file to resolve whether the current video has a Noir Mode preset override.
5. If no preset applies, the wrapper calls real FFmpeg with the original arguments.
6. If a preset applies, the wrapper injects the preset filter into the video filter chain.
7. The wrapper calls real FFmpeg with modified arguments.

### Filter Injection Rules

Initial support should handle common Jellyfin transcode commands:

- If `-vf` exists, append the noir filter with a comma.
- If no `-vf` exists, insert `-vf <filter>` before the output path.
- If video codec is stream copy, skip injection and log a clear `video-stream-copy-unsupported` decision.
- Preserve audio, subtitle, mapping, bitrate, and output arguments.

Initial version should not modify complex filter graphs. If the command contains `-filter_complex`, the wrapper should log that noir mode was skipped for that playback unless explicit support has been implemented and tested.

### Hardware Acceleration

MVP should prioritize correctness over hardware acceleration:

- Support software filter insertion first.
- Detect hardware-accelerated filter chains and skip or fall back safely.
- Add hardware-specific filter support later for VAAPI, QSV, CUDA, and VideoToolbox only after integration testing.

## Client Behavior

### Universal Behavior

All clients should work because they receive normal Jellyfin output after server-side transcoding.

Clients without noir-specific UI should use the per-video setting saved on the server. If the video has no Noir Mode override, playback stays normal.

For noir mode to apply, playback must transcode video. Direct Play and Direct Stream will not run FFmpeg video filters. The plugin should provide admin guidance for forcing transcoding where needed, but the MVP should not claim it can force every client to transcode until that capability has been proven.

### Optional Jellyfin Web Video Page Selector

Add a Jellyfin Web enhancement only after the server-side flow is working:

- Add a `Noir Mode` row/dropdown on video item pages near the existing Video, Audio, and Subtitles selectors.
- Show the selector only for video files/items.
- Style and position the selector like Jellyfin Web's existing stream selectors, but do not represent Noir Mode as a subtitle track.
- Include `Off` plus the built-in Noir Mode presets.
- Write the selected mode through the plugin API.
- Save or clear the current video's item override through the plugin API.

This does not automatically cover Android, iOS, Roku, Android TV, Swiftfin, Finamp, Infuse, Kodi, or other clients. Those clients rely on saved per-video server-side overrides.

## API Endpoints

Suggested endpoints:

- `GET /NoirMode/config`
- `POST /NoirMode/config`
- `GET /NoirMode/presets`
- `GET /NoirMode/items/search?query=...`
- `GET /NoirMode/items/{itemId}/override`
- `PUT /NoirMode/items/{itemId}/override`
- `DELETE /NoirMode/items/{itemId}/override`
- `GET /NoirMode/wrapper/status`
- `POST /NoirMode/wrapper/install`
- `POST /NoirMode/wrapper/test`
- `GET /NoirMode/resolve?itemId=...`

The wrapper should avoid depending on authenticated browser cookies. For wrapper-to-plugin communication, prefer the state file approach in the MVP:

- A state file written by the plugin and read by the wrapper.

A local-only endpoint protected by a generated shared secret can be added later if the state file is not expressive enough. The state file approach is simpler and avoids HTTP authentication problems inside an FFmpeg wrapper process.

## Existing Plugin References

Useful existing projects:

- Jellyfin plugin template: `https://github.com/jellyfin/jellyfin-plugin-template`
- Jellyfin plugin docs: `https://jellyfin.org/docs/general/server/plugins/`
- Plugin repository manifest docs: `https://jellyfin.org/posts/plugin-updates/`
- TranscodeKiller: `https://github.com/jellyfin/jellyfin-plugin-transcodekiller`
- Jellyfin AI Upscaler plugin: `https://github.com/Kuschel-code/JellyfinUpscalerPlugin`
- rffmpeg wrapper pattern: `https://github.com/joshuaboniface/rffmpeg`

Research finding: TranscodeKiller interacts with Jellyfin transcode jobs, but kills or blocks transcodes rather than adding FFmpeg video filters. The AI Upscaler plugin contains useful wrapper and FFmpeg command-building patterns, but it is not a clean general-purpose hook into Jellyfin's native transcoding pipeline for every client.

## Testing Plan

### Unit Tests

Cover:

- Preset validation.
- Rule precedence.
- Config serialization.
- FFmpeg argument parsing.
- `-vf` injection.
- No existing `-vf` injection.
- Stream-copy video handling.
- `-filter_complex` skip behavior.
- Windows and Unix argument quoting.

### Integration Tests

Use a Jellyfin test server with sample media:

1. Install the plugin.
2. Configure the wrapper.
3. Enable the plugin.
4. Set a Noir Mode preset override on one test video.
5. Start playback from Jellyfin Web.
6. Confirm the transcode log includes the noir filter for that video.
7. Confirm a video without an override transcodes without noir filters.
8. Confirm output is grayscale/noir for the overridden video.
9. Repeat with at least one non-web client or direct stream URL.
10. Confirm disabled mode passes FFmpeg arguments through unchanged.

### Compatibility Tests

Test combinations:

- Windows server.
- Linux server.
- Docker server.
- HLS playback.
- Progressive playback.
- Direct Play disabled.
- Direct Play enabled, confirming noir mode does not apply.
- Hardware acceleration enabled and disabled.
- Subtitle burn-in cases.

## Milestones

### Milestone 1: Scaffold

- Create plugin from Jellyfin template.
- Add GPLv3 metadata.
- Add plugin manifest.
- Add empty config page.
- Build against target Jellyfin SDK.

### Milestone 2: Presets And Rules

- Implement built-in presets.
- Implement config persistence.
- Implement plugin enablement and preset registry.
- Implement per-video item override model and precedence.

### Milestone 2.5: Per-Video Overrides

- Add item override API endpoints.
- Add searchable item override management in the admin page.
- Support three states: disabled, off, and specific preset.
- Export per-video overrides to the wrapper-readable state file.
- Verify videos without per-video preset overrides remain normal.

### Milestone 3: Wrapper Pass-Through

- Build Windows and Unix wrapper executables.
- Store real FFmpeg path.
- Verify pass-through transcoding is identical before filter injection.
- Add wrapper status diagnostics.

### Milestone 4: Filter Injection

- Parse FFmpeg args safely.
- Inject simple `-vf` filters.
- Skip unsupported complex filter graphs.
- Add structured logging for every decision.

### Milestone 5: Admin Workflow

- Add wrapper install/test UI.
- Add clear setup instructions for setting Jellyfin's FFmpeg path.
- Add warnings about Direct Play and hardware acceleration.

### Milestone 6: Optional Web Player UI

- Add Jellyfin Web button and preset selector.
- Persist selection through plugin API.
- Treat this as optional because it does not cover every client.

### Milestone 7: Packaging

- Produce plugin ZIP artifacts.
- Generate repository JSON.
- Publish GitHub Release.
- Document supported Jellyfin versions.

## Risks

- Jellyfin does not currently provide a stable public plugin hook for modifying native FFmpeg transcode arguments.
- Wrapper-based user/session detection may be limited.
- Direct Play bypasses FFmpeg and cannot receive server-side video filters.
- Subtitle burn-in can produce `-filter_complex`, which the MVP should skip unless explicitly supported.
- Hardware acceleration filter chains are platform-specific and need careful testing.
- Custom FFmpeg filters are a security risk and should stay disabled until a strong validator exists.

## Long-Term Improvement

The clean long-term design is a Jellyfin core extension point such as:

```csharp
public interface ITranscodingFilterProvider
{
    bool AppliesTo(TranscodingFilterContext context);
    IReadOnlyList<string> GetVideoFilters(TranscodingFilterContext context);
}
```

Jellyfin's transcoding pipeline would call registered providers before final FFmpeg command generation. Noir Mode would then implement this provider instead of using a wrapper. This would make per-session and per-user behavior more reliable and avoid argument rewriting.
