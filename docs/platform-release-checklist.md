# Platform Release Checklist

## Shared

- Build Release with `scripts/build-release.ps1`.
- Verify `ApplicationDisplayVersion`, `ApplicationVersion`, and `Directory.Build.props`.
- Test first launch, level selection, play, restore best, settings, offline play, and leaderboard sync.
- Confirm privacy policy URL and support contact are ready for store listings.
- Verify app icon and splash render at target sizes.

## Android

- Build and install a signed Release APK or AAB.
- Test touch drag, pinch zoom, scroll, app pause/resume, and network loss.
- Confirm package id is `com.adamkurek.fsquir`.
- Confirm `INTERNET` and `ACCESS_NETWORK_STATE` are still the only required permissions.

## Windows

- Build `net10.0-windows10.0.19041.0` Release.
- Test mouse drag, middle-button pan, scroll zoom, window resizing, high DPI, and suspend/resume.
- Replace package publisher placeholders when a signing certificate is selected.

## iOS and Mac Catalyst

- Build on macOS with the matching .NET workloads.
- Test touch gestures, safe areas, app lifecycle, and offline progress persistence.
- Confirm bundle id, signing team, privacy manifest requirements, and store screenshots.
