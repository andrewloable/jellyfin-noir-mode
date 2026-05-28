# Jellyfin Noir Mode Plugin

A Jellyfin plugin that adds real-time Noir Mode playback using FFmpeg filters. It lets selected videos play in cinematic black-and-white styles without modifying the original media files.

## Features

* Per-video Noir Mode presets, disabled by default.
* Server-side FFmpeg filtering during Jellyfin video transcoding.
* Bundled FFmpeg wrapper for Windows, Linux, Docker, and macOS servers.
* Normal Jellyfin playback output, so supported clients do not need a Noir Mode-specific client plugin.
* Original media files are never modified.

## Presets

* Classic B&W
* Film Noir
* High Contrast Noir
* Vintage Noir
* Soft Noir

## Requirements

* Jellyfin Server `10.11.0.0`
* Jellyfin FFmpeg
* A playback path that uses server-side video transcoding

## Installation

### 1. Install The Plugin

In Jellyfin, open Dashboard, then Plugins, then Repositories. Add the repository URL for the OS running your Jellyfin server:

```text
Windows:
https://raw.githubusercontent.com/andrewloable/jellyfin-noir-mode/main/manifest-windows-x64.json

Linux or Docker:
https://raw.githubusercontent.com/andrewloable/jellyfin-noir-mode/main/manifest-linux-x64.json

macOS:
https://raw.githubusercontent.com/andrewloable/jellyfin-noir-mode/main/manifest-macos.json
```

Save the repository, install Noir Mode from the plugin catalog, then restart Jellyfin.

Manual install is also supported. Download the matching release ZIP and extract it to Jellyfin's plugin directory:

```text
Windows: jellyfin-plugin-noir-mode-windows-x64-0.1.0.zip
Linux/Docker: jellyfin-plugin-noir-mode-linux-x64-0.1.0.zip
macOS: jellyfin-plugin-noir-mode-macos-0.1.0.zip
```

### 2. Configure The FFmpeg Wrapper

Each OS-specific bundle includes the matching wrapper under the plugin folder. The plugin can configure Jellyfin to use it automatically:

1. Open the Noir Mode plugin settings.
2. Click **Use bundled wrapper**.
3. Restart Jellyfin.

That action saves Jellyfin's current FFmpeg path as the real FFmpeg binary, points Jellyfin's FFmpeg setting at the bundled wrapper, and writes the wrapper config file.

Docker plugin repository installs do not need a separate wrapper mount. Jellyfin downloads the plugin into `/config/plugins`, and the plugin uses that installed wrapper path.

Linuxserver.io Jellyfin images launch Jellyfin with a container-level `--ffmpeg="${FFMPEG_PATH}"` argument. For those images, set `FFMPEG_PATH` in the container environment to the installed Noir Mode wrapper path after installing the plugin, then restart the container:

```yaml
environment:
  - FFMPEG_PATH=/config/data/plugins/Noir Mode_0.1.0.0/wrapper/linux-x64/Jellyfin.Plugin.NoirMode.Wrapper
```

### 3. Enable Noir Mode

1. Open Jellyfin Dashboard.
2. Go to Plugins.
3. Open Noir Mode.
4. Search for a video.
5. Set that video's Noir Mode preset.

Noir Mode applies only to videos with a per-video preset override. Videos are disabled by default.

## Usage

After the wrapper is configured, Noir Mode is selected per video. Videos without a saved Noir Mode preset continue to play normally.

### Jellyfin Web

1. Open a video details page in the server-hosted Jellyfin Web UI.
2. Use the **Noir Mode** selector near the Video, Audio, and Subtitles selectors.
3. Choose **Off** or one of the built-in Noir Mode presets.
4. Play the video through a Jellyfin playback path that transcodes video.

The Jellyfin Web selector saves the video's server-side Noir Mode override. Other Jellyfin clients use that saved setting when playback transcodes video, even if they do not show a Noir Mode selector.

For Docker installs, Jellyfin Web's `index.html` may be owned by `root`, while Jellyfin runs as a non-root user. If the selector does not appear and the Jellyfin log says Noir Mode could not update `index.html` because access was denied, run the web integration helper on the Docker host and hard-refresh Jellyfin Web:

```bash
./scripts/install-web-integration.sh jellyfin
docker restart jellyfin
```

From PowerShell:

```powershell
.\scripts\install-web-integration.ps1 -Container jellyfin
docker restart jellyfin
```

### Plugin Settings

Administrators can also manage per-video overrides from the Noir Mode plugin settings page:

1. Open Jellyfin Dashboard.
2. Go to Plugins, then open Noir Mode.
3. Search for a video.
4. Select a Noir Mode preset or clear the override.

Plugin configuration, wrapper setup, wrapper rollback, and override management require an elevated Jellyfin administrator session.

## Rollback

1. Open the Noir Mode plugin settings.
2. Click **Restore real FFmpeg**.
3. Restart Jellyfin.
4. Disable or remove the Noir Mode plugin.

No media files are modified by Noir Mode.

## Limitations

* Direct Play and Direct Stream bypass FFmpeg filters.
* Noir Mode may require forcing video transcoding for specific clients or media.
* Real-time filtering can increase CPU or GPU usage.
* Stream copy, complex filter graphs, and unsupported hardware filter chains are passed through unchanged.
* Custom FFmpeg filters are not supported.
* The Jellyfin Web video-page dropdown is available only in Jellyfin Web clients that load the server-hosted web UI.

## Troubleshooting

For setup issues, check the Jellyfin server log for `Noir Mode` entries. The plugin logs wrapper detection, FFmpeg path changes, wrapper config writes, rollback, item override changes, and state exports.

For playback issues, check the Jellyfin transcode log or FFmpeg stderr for `NoirModeWrapper` entries. The wrapper logs its config path, real FFmpeg path, state file path, filter injection decision, and real FFmpeg exit code.

## License

This project is licensed under the GNU General Public License v3.0.

See the [LICENSE](LICENSE) file for details.

## Disclaimer

This project is experimental and is not affiliated with or endorsed by Jellyfin.

Jellyfin is a trademark of its respective owners.
