# Packaging

MVP 1 ships two artifacts:

- `Jellyfin.Plugin.NoirMode` plugin files.
- `Jellyfin.Plugin.NoirMode.Wrapper` native executable for the server runtime.

## Build

```powershell
dotnet test Jellyfin.Plugin.NoirMode.slnx
pwsh ./scripts/package.ps1
```

## Install

1. Copy plugin files from `artifacts/plugin` to Jellyfin's plugin directory.
2. Copy the wrapper executable from `artifacts/wrapper/<runtime>` to a stable path.
3. Restart Jellyfin.
4. Open the Noir Mode plugin settings.
5. Set the real FFmpeg path.
6. Set Jellyfin's FFmpeg path to the wrapper executable.
7. Set wrapper environment variables:
   - `NOIR_REAL_FFMPEG`
   - `NOIR_STATE_FILE`
8. Enable the plugin and configure per-video overrides.

## Rollback

1. Set Jellyfin's FFmpeg path back to the real FFmpeg binary.
2. Restart Jellyfin.
3. Disable or remove the Noir Mode plugin.

No media files are modified by Noir Mode.
