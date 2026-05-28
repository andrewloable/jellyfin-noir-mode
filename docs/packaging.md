# Packaging

MVP 1 ships separate plugin artifacts by Jellyfin server OS:

- `jellyfin-plugin-noir-mode-windows-x64-0.1.0.zip`
- `jellyfin-plugin-noir-mode-linux-x64-0.1.0.zip`
- `jellyfin-plugin-noir-mode-macos-0.1.0.zip`

Each plugin ZIP includes the same plugin files plus only the wrapper binaries for that server OS:

- `wrapper/win-x64`
- `wrapper/linux-x64`
- `wrapper/osx-x64` and `wrapper/osx-arm64`

The macOS package contains both Intel and Apple Silicon wrapper builds.

Jellyfin plugin repository manifests have one `sourceUrl` per version and cannot choose an artifact by server OS. The release manifest points at the Linux x64 package as the default catalog artifact; Windows and macOS installs should use the matching GitHub release ZIP manually.

## Build

```powershell
dotnet test Jellyfin.Plugin.NoirMode.slnx
pwsh ./scripts/package.ps1
```

## Install

1. Extract the plugin ZIP for the Jellyfin server OS to Jellyfin's plugin directory.
2. Restart Jellyfin.
3. Open the Noir Mode plugin settings.
4. Set the real FFmpeg path.
5. Set Jellyfin's FFmpeg path to the bundled wrapper executable inside the plugin directory.
6. Set wrapper environment variables:
   - `NOIR_REAL_FFMPEG`
   - `NOIR_STATE_FILE`
7. Enable the plugin and configure per-video overrides.

## Rollback

1. Set Jellyfin's FFmpeg path back to the real FFmpeg binary.
2. Restart Jellyfin.
3. Disable or remove the Noir Mode plugin.

No media files are modified by Noir Mode.
