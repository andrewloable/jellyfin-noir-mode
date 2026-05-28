# Jellyfin Noir Mode Plugin

A Jellyfin plugin that adds real-time Noir Mode playback using FFmpeg filters. Watch movies and shows in cinematic black-and-white with optional high contrast, vintage styling, and non-destructive server-side video effects without modifying your original media files.

> Status: MVP implementation scaffold

---

## Overview

Jellyfin Noir Mode is intended to provide a stylized black-and-white viewing mode for Jellyfin media playback.

The plugin aims to apply FFmpeg video filters during transcoding, allowing users to experience selected videos in grayscale, high-contrast monochrome, or vintage noir-inspired styles without permanently converting or editing the original media files.

This project currently contains a buildable Jellyfin plugin project, FFmpeg wrapper executable, shared core logic, tests, and packaging script.

---

## Goals

* Add a user-selectable Noir Mode for Jellyfin playback
* Apply black-and-white video effects in real time
* Use FFmpeg filters during Jellyfin transcoding
* Keep original media files untouched
* Support multiple visual presets
* Provide a simple configuration interface
* Support per-video Noir Mode preferences, disabled by default

---

## Planned Features

### Core Features

* Real-time grayscale video conversion during supported transcodes
* High-contrast black-and-white mode
* Allowlisted FFmpeg filter chains
* Server-side, non-destructive video processing
* Per-video Noir Mode disabled by default
* Plugin configuration page inside Jellyfin

### Possible Presets

* `Classic B&W`
* `Film Noir`
* `High Contrast`
* `Vintage Cinema`
* `Old Film Grain`
Custom FFmpeg filters are intentionally disabled for MVP 1.

### Future Ideas

* Per-movie override
* Toggle from playback UI
* Film grain intensity control
* Brightness and contrast sliders
* Client-side compatibility notes
* Hardware transcoding compatibility testing

---

## Example FFmpeg Filters

Basic grayscale:

```bash
-vf format=gray
```

Desaturated grayscale:

```bash
-vf hue=s=0
```

Noir-style high contrast:

```bash
-vf "hue=s=0,eq=contrast=1.35:brightness=-0.03"
```

Vintage black-and-white with grain:

```bash
-vf "hue=s=0,eq=contrast=1.25:brightness=-0.02,noise=alls=12:allf=t+u"
```

---

## Intended Architecture

```text
Jellyfin Playback Request
        |
        v
Plugin Noir Mode Setting
        |
        v
Transcoding Decision
        |
        v
FFmpeg Filter Injection
        |
        v
Real-time Noir Playback
```

The plugin will investigate whether Jellyfin’s transcoding pipeline can be extended or modified to append custom FFmpeg video filters during playback.

---

## Configuration Concept

Example future configuration:

```json
{
  "enabled": true,
  "allowCustomFilters": false,
  "itemOverrides": {
    "jellyfin-item-id": "film-noir"
  },
  "presets": {
    "classic-bw": "hue=s=0",
    "film-noir": "hue=s=0,eq=contrast=1.35:brightness=-0.03",
    "vintage": "hue=s=0,eq=contrast=1.25:brightness=-0.02,noise=alls=12:allf=t+u"
  }
}
```

---

## Development Status

Current phase:

* [x] Project concept
* [x] Jellyfin plugin structure
* [x] Configuration model
* [x] Admin settings page
* [x] Native FFmpeg wrapper
* [x] Noir preset system
* [x] Unit tests
* [x] Release packaging script
* [ ] Live Jellyfin server compatibility validation

---

## Requirements

Planned requirements:

* Jellyfin Server
* Jellyfin plugin development environment
* .NET SDK
* FFmpeg / Jellyfin FFmpeg
* A Jellyfin server capable of transcoding media

MVP 1 targets Jellyfin `10.11.0.0` ABI and .NET `net9.0`.

---

## Installation

Download these files from the GitHub release:

* Windows server: `jellyfin-plugin-noir-mode-windows-x64-0.1.0.zip`
* Linux server or Docker: `jellyfin-plugin-noir-mode-linux-x64-0.1.0.zip`
* macOS server: `jellyfin-plugin-noir-mode-macos-0.1.0.zip`
* `checksums.txt`

Each plugin ZIP includes the plugin files plus the wrapper binaries for that server OS. The macOS ZIP includes both `osx-x64` and `osx-arm64` wrapper builds.

Jellyfin plugin repository manifests cannot select a different ZIP by server OS. For Windows and macOS servers, install manually from the matching GitHub release ZIP.

Verify the downloaded files against `checksums.txt` before installing.

### 1. Install The Plugin

Extract the ZIP for your Jellyfin server OS into a dedicated Jellyfin plugin directory.

Typical plugin locations:

```text
Windows: C:\ProgramData\Jellyfin\Server\plugins\Noir Mode
Linux:   /var/lib/jellyfin/plugins/Noir Mode
Docker:  /config/plugins/Noir Mode
```

Restart Jellyfin after copying the plugin files.

### 2. Install The Wrapper

The wrapper is included inside the extracted plugin folder. You can use it in place from the plugin directory.

```text
Windows example: C:\ProgramData\Jellyfin\Server\plugins\Noir Mode\wrapper\win-x64\Jellyfin.Plugin.NoirMode.Wrapper.exe
Linux example:   /var/lib/jellyfin/plugins/Noir Mode/wrapper/linux-x64/Jellyfin.Plugin.NoirMode.Wrapper
macOS Intel:     /var/lib/jellyfin/plugins/Noir Mode/wrapper/osx-x64/Jellyfin.Plugin.NoirMode.Wrapper
macOS Apple:     /var/lib/jellyfin/plugins/Noir Mode/wrapper/osx-arm64/Jellyfin.Plugin.NoirMode.Wrapper
```

On Linux and macOS, make the wrapper executable if needed:

```bash
chmod +x "/var/lib/jellyfin/plugins/Noir Mode/wrapper/linux-x64/Jellyfin.Plugin.NoirMode.Wrapper"
chmod +x "/var/lib/jellyfin/plugins/Noir Mode/wrapper/osx-x64/Jellyfin.Plugin.NoirMode.Wrapper"
chmod +x "/var/lib/jellyfin/plugins/Noir Mode/wrapper/osx-arm64/Jellyfin.Plugin.NoirMode.Wrapper"
```

For Docker, the wrapper is mounted with the plugin folder. The wrapper path you configure in Jellyfin must be the path inside the container, not the host path.

Example host layout:

```text
./jellyfin/
  config/
  cache/
  media/
  Noir Mode/
    wrapper/
      linux-x64/
        Jellyfin.Plugin.NoirMode.Wrapper
```

Example Docker volume mounts:

```yaml
volumes:
  - ./config:/config
  - ./cache:/cache
  - ./media:/media:ro
  - ./Noir Mode:/config/plugins/Noir Mode:ro
```

Inside Jellyfin Dashboard, the wrapper path is:

```text
/config/plugins/Noir Mode/wrapper/linux-x64/Jellyfin.Plugin.NoirMode.Wrapper
```

### 3. Configure Jellyfin To Use The Wrapper

In Jellyfin Dashboard, set the FFmpeg path to the wrapper executable, not the real FFmpeg binary.

Keep the original Jellyfin FFmpeg path. You will need it for `NOIR_REAL_FFMPEG`.

### 4. Set Wrapper Environment Variables

The wrapper needs two environment variables in the Jellyfin server process:

```text
NOIR_REAL_FFMPEG=<path to the real Jellyfin FFmpeg binary>
NOIR_STATE_FILE=<path to jellyfin-noir-mode-state.json>
```

Examples:

```text
Windows:
NOIR_REAL_FFMPEG=C:\Program Files\Jellyfin\Server\ffmpeg.exe
NOIR_STATE_FILE=C:\ProgramData\Jellyfin\Server\plugins\configurations\jellyfin-noir-mode-state.json

Linux:
NOIR_REAL_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg
NOIR_STATE_FILE=/var/lib/jellyfin/plugins/configurations/jellyfin-noir-mode-state.json

Docker:
NOIR_REAL_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg
NOIR_STATE_FILE=/config/plugins/configurations/jellyfin-noir-mode-state.json
```

Restart Jellyfin after changing environment variables.

For Docker, set these environment variables on the Jellyfin container:

Example `docker-compose.yml` fragment:

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin:10.11.0
    ports:
      - "8096:8096"
    environment:
      - NOIR_REAL_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg
      - NOIR_STATE_FILE=/config/plugins/configurations/jellyfin-noir-mode-state.json
    volumes:
      - ./config:/config
      - ./cache:/cache
      - ./media:/media:ro
      - ./Noir Mode:/config/plugins/Noir Mode:ro
```

If your Jellyfin image uses a different FFmpeg path, update `NOIR_REAL_FFMPEG` to match that container path.

### 5. Enable Noir Mode

1. Open Jellyfin Dashboard.
2. Go to Plugins.
3. Open Noir Mode.
4. Enable wrapper support.
5. Confirm the real FFmpeg path and wrapper path.
6. Search for a video.
7. Set that video's Noir Mode preset.

Every video is disabled by default. Noir Mode applies only to videos with a per-video preset override.

### Rollback

1. Set Jellyfin's FFmpeg path back to the real FFmpeg binary.
2. Restart Jellyfin.
3. Disable or remove the Noir Mode plugin.

No media files are modified by Noir Mode.

---

## Usage

Usage flow:

```text
1. Install the plugin
2. Open Jellyfin Dashboard
3. Go to Plugins
4. Configure wrapper paths
5. Search for a video item
6. Set a per-video Noir Mode preset override
7. Start playback with transcoding enabled
```

---

## Limitations

Expected limitations:

* May require transcoding instead of direct play
* May increase CPU or GPU usage
* Hardware acceleration compatibility needs testing
* Some Jellyfin clients may not expose a playback toggle
* Real-time filtering depends on Jellyfin invoking FFmpeg for video transcoding
* Stream copy, complex filter graphs, and unsupported hardware filter chains are passed through unchanged
* Custom FFmpeg filters are disabled in MVP 1

---

## Development Notes

This plugin will likely need to investigate Jellyfin internals related to:

* Transcoding profiles
* Media playback pipeline
* FFmpeg argument generation
* Plugin configuration pages
* Per-video settings
* Client playback controls

Alternative approaches may include:

* FFmpeg wrapper script
* Custom transcoding profile
* Server-side plugin integration
* Client-side shader/filter support
* External transcoding service

---

## Contributing

Contributions, ideas, and technical research are welcome.

Useful areas for contribution:

* Jellyfin plugin development
* FFmpeg filter chains
* Transcoding pipeline research
* UI/UX design for playback toggles
* Hardware acceleration testing
* Cross-client compatibility testing

---

## License

This project is licensed under the GNU General Public License v3.0.

See the [LICENSE](LICENSE) file for details.

---

## Disclaimer

This project is experimental and is not affiliated with or endorsed by Jellyfin.

Jellyfin is a trademark of its respective owners.
