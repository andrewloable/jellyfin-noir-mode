# Integration Harness

This harness documents the repeatable Jellyfin test path for MVP 1.

## Prerequisites

- Docker or another local Jellyfin test server.
- FFmpeg available on the host.
- A short test video in `tests/integration/media`.
- Built plugin artifacts from `dotnet publish`.

## Test Matrix

1. Install the Noir Mode plugin into the Jellyfin test server.
2. Publish `Jellyfin.Plugin.NoirMode.Wrapper` for the test host runtime.
3. Set Jellyfin's FFmpeg path to the wrapper executable.
4. Set wrapper environment:
   - `NOIR_REAL_FFMPEG=<path-to-real-ffmpeg>`
   - `NOIR_STATE_FILE=<path-to-jellyfin-noir-mode-state.json>`
5. Enable the plugin in the admin page.
6. Search for a test video and set its per-video override to `classic-bw`.
7. Play the overridden video with transcoding enabled.
8. Confirm Jellyfin logs contain a wrapper decision with `applied=True`.
9. Play a second video without an override.
10. Confirm wrapper logs show `reason=no-item-override` and the FFmpeg args are unchanged.
11. Test a Direct Play path and confirm Noir Mode does not apply.

## Expected Results

- Overridden video: safe software transcode commands include the noir `-vf` filter.
- No override: playback remains unchanged.
- Stream copy, `-filter_complex`, and hardware chains: wrapper passes through unchanged and logs a skip reason.

## Docker Notes

Docker path mapping must match between Jellyfin and the wrapper state. If Jellyfin sees media as `/media/movie.mkv`, the per-video override state must include that normalized path or a matching path hash.
