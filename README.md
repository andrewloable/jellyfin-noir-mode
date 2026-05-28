# Jellyfin Noir Mode Plugin

A Jellyfin plugin that adds real-time Noir Mode playback using FFmpeg filters. Watch movies and shows in cinematic black-and-white with optional high contrast, vintage styling, and non-destructive server-side video effects without modifying your original media files.

> Status: Pre-implementation / proof-of-concept planning

---

## Overview

Jellyfin Noir Mode is intended to provide a stylized black-and-white viewing mode for Jellyfin media playback.

The plugin aims to apply FFmpeg video filters during transcoding, allowing users to experience selected videos in grayscale, high-contrast monochrome, or vintage noir-inspired styles without permanently converting or editing the original media files.

This project is currently in the planning and scaffolding stage.

---

## Goals

* Add a user-selectable Noir Mode for Jellyfin playback
* Apply black-and-white video effects in real time
* Use FFmpeg filters during Jellyfin transcoding
* Keep original media files untouched
* Support multiple visual presets
* Provide a simple configuration interface
* Support per-video Noir Mode preferences

---

## Planned Features

### Core Features

* Real-time grayscale video conversion
* High-contrast black-and-white mode
* Configurable FFmpeg filter chains
* Server-side, non-destructive video processing
* Per-video Noir Mode disabled by default
* Plugin configuration page inside Jellyfin

### Possible Presets

* `Classic B&W`
* `Film Noir`
* `High Contrast`
* `Vintage Cinema`
* `Old Film Grain`
* `Custom FFmpeg Filter`

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

This project is not yet implemented.

Current phase:

* [x] Project concept
* [x] README scaffold
* [ ] Jellyfin plugin structure
* [ ] Configuration model
* [ ] Admin settings page
* [ ] FFmpeg filter integration research
* [ ] Noir preset system
* [ ] Playback testing
* [ ] Release packaging

---

## Requirements

Planned requirements:

* Jellyfin Server
* Jellyfin plugin development environment
* .NET SDK
* FFmpeg / Jellyfin FFmpeg
* A Jellyfin server capable of transcoding media

Exact version requirements will be added after implementation begins.

---

## Installation

Installation instructions will be added once the plugin is implemented and packaged.

Planned options:

```text
1. Download plugin release
2. Copy plugin files to Jellyfin plugin directory
3. Restart Jellyfin
4. Enable and configure Noir Mode
```

---

## Usage

Usage instructions will be added after the first working version.

Planned usage flow:

```text
1. Install the plugin
2. Open Jellyfin Dashboard
3. Go to Plugins
4. Configure Noir Mode
5. Start playback with transcoding enabled
6. Enjoy black-and-white playback
```

---

## Limitations

Expected limitations:

* May require transcoding instead of direct play
* May increase CPU or GPU usage
* Hardware acceleration compatibility needs testing
* Some Jellyfin clients may not expose a playback toggle
* Real-time filtering may depend on Jellyfin’s transcoding internals
* Custom FFmpeg filters may break playback if invalid

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
