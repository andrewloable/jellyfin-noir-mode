# GitHub Release v0.1.0

## Title

Jellyfin Noir Mode v0.1.0

## Summary

Initial MVP release of Jellyfin Noir Mode. This release provides OS-specific Jellyfin plugin ZIPs with bundled native FFmpeg wrappers, built-in noir presets, and per-video Noir Mode overrides that are disabled by default.

## Release Notes

- Adds Jellyfin plugin scaffold targeting Jellyfin `10.11.0.0` ABI and `.NET net9.0`.
- Adds bundled native FFmpeg wrapper builds for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`.
- Adds per-video Noir Mode override settings.
- Defaults every video to disabled/no Noir Mode.
- Adds built-in allowlisted presets:
  - Classic B&W
  - Film Noir
  - High Contrast Noir
  - Vintage Noir
  - Soft Noir
- Injects noir filters only into safe software video transcode commands.
- Passes through unsupported commands unchanged, including stream copy, `-filter_complex`, and likely hardware filter chains.
- Adds admin configuration page and plugin API endpoints.
- Adds packaging script, release manifest, integration harness docs, and automated tests.

## Assets To Upload

- `jellyfin-plugin-noir-mode-windows-x64-0.1.0.zip`
- `jellyfin-plugin-noir-mode-linux-x64-0.1.0.zip`
- `jellyfin-plugin-noir-mode-macos-0.1.0.zip`
- `manifest-windows-x64.json`
- `manifest-linux-x64.json`
- `manifest-macos.json`
- `checksums.txt`

## Known Limitations

- Direct Play and Direct Stream bypass FFmpeg filters.
- Hardware-accelerated filter chains are skipped in MVP 1.
- `-filter_complex` commands are skipped in MVP 1.
- Jellyfin's FFmpeg path must be pointed at the wrapper executable.
- Wrapper environment variables must be configured:
  - `NOIR_REAL_FFMPEG`
  - `NOIR_STATE_FILE`
