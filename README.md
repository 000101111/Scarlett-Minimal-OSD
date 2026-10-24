# Scarlett Minimal OSD

**English · [简体中文](README.zh-CN.md)**

Minimal Windows monitoring overlay and convenient control for **Focusrite Scarlett 16i16 (4th Gen)** through Focusrite Control 2 (FC2).

**English + 简体中文 UI** · [Download the compiled preview](https://github.com/000101111/Scarlett-Minimal-OSD/releases/tag/v0.1.1-alpha) · [Make your own language version with AI](docs/LOCALIZATION.md)

Want another language? Give your AI coding assistant the [ready-to-copy localization prompt](docs/LOCALIZATION.md). All interface strings are grouped in language resources.

> **Unofficial, independent community project.** Not affiliated with or endorsed by Focusrite. "Focusrite" and "Scarlett" are trademarks of their owner. The Windows Release build passes on GitHub Actions. Hardware behavior has **not** been independently tested here; the original contributor reports successful operation on their setup. Other Scarlett models are **not verified**.

## At a glance

A borderless black **280 × 100** on-screen display: only **MAIN / ALT**, the actual hardware monitor gain in **dB**, and **MUTE / DIM** indicators when active. The selected monitor group uses red; large gain digits use white.

### Controls confirmed in source

| Action | Result |
| --- | --- |
| Turn physical Monitor dial | Display updated device-reported monitor gain |
| Press physical ALT button | Display MAIN / ALT state |
| Click MAIN or ALT in OSD | Request hardware monitor-group change |
| Use mouse wheel over Windows notification area | Request monitor gain adjustment (see **known issue** below) |
| Double-click **this app's** tray icon | Toggle the **Focusrite Control 2 GUI** visible / hidden |
| Click the empty part / gain display of OSD | Restore the official FC2 window |
| Right-click tray icon | FC2 show/hide, OSD settings, exit |
| Change settings | Adjust transparency, display duration, gain step, gain limit, FC2 launch and exit behaviour |

The FC2 application remains running when its GUI is hidden; only its window visibility is changed, not its audio driver. The overlay defaults to the **right side at vertical centre**, and disappears after a short pause.

## Requirements

- Windows 10/11 x64 (Windows 11 is the intended platform)
- Focusrite Control 2 and a compatible Scarlett device; **16i16 4th Gen** is the known target
- .NET 8 Desktop Runtime (for provided framework-dependent EXE)
- Node.js >= 20 (the prebuilt portable preview bundles Node.js)
- First-time FC2 API authorisation / pairing is required on a new Windows profile unless an authorised identity already exists. **Automatic Windows onboarding is not implemented in this packaged OSD bridge** (its bundled discovery helper invokes Unix `lsof`); the bridge uses the `focusrite-control-2-api` FC2 backend and the local `%APPDATA%\focusrite-control-2-api` client identity/configuration; do not upload those private files.

## For Windows users

1. Download **Scarlett-Minimal-OSD-Windows-x64-Preview.zip** from [the preview release](https://github.com/000101111/Scarlett-Minimal-OSD/releases/tag/v0.1.1-alpha). This contains a compiled EXE; users do not need the SDK or to build the application.
2. Extract it to a writable folder. Make sure `ScarlettMonitorOSD.exe`, `node.exe`, and `bridge/` stay together.
3. Install .NET 8 Desktop Runtime if Windows requests it. Start the official Focusrite Control 2. If this is a new PC/profile, the compact bridge may require manual pairing using the upstream Focusrite Control API tools; it does not automatically pop up a pairing wizard.
4. Run `ScarlettMonitorOSD.exe`. Use its tray icon to toggle the FC2 window.
5. Start with speakers muted or monitoring gain low while checking volume write controls.

**Troubleshooting:** If the OSD says offline, verify FC2 is running and the bridge has valid pairing/config in the user's own profile. Diagnostic logs may contain local connection data; do not post them without review.

## Building from source

Clone or download this repository, then on Windows run `build.cmd` (or `powershell -ExecutionPolicy Bypass -File ./build.ps1`). It requires **.NET 8 SDK** and **Node.js 20+**; build script installs npm dependencies from `bridge/package.json`. Output is written to `dist/ScarlettMonitorOSD` so it doesn't overwrite a running copy. Windows GitHub Actions also provides build artifacts.

The executable is a single published WPF entry point but the software is **not one-file standalone**: it also requires `bridge/` and Node, plus the .NET Desktop Runtime. Do not upload your `node_modules/` or `dist/` into the source repository.

## Security and limitations

- The app can **write monitor gain** and **switch monitor groups**. Do not use it unattended while testing. New-user Windows pairing/provisioning is not fully automated in this version.
- **Known issue in supplied build:** the low-level wheel hook checks the **Windows tray area**, not just the OSD's own icon. Scrolling over another tray icon may change monitor gain. Avoid using wheel control until this is corrected.
- The UI sets a configurable maximum gain (default 0 dB) but the Node bridge has a less restrictive internal maximum. Treat the built-in bridge as trusted software; do **not** expose its standard input or any experimental local API to untrusted clients.
- FC2 main-window discovery uses a title / class-name heuristic. It may break when FC2 changes its UI or window class.
- This does not add an ASIO DSP, hardware EQ, virtual surround, or sample-rate/peak meter. Those are separate ideas, **not** present in this release.
- No OS-level driver or Focusrite firmware modification is required by the FC2 backend.

## Language / localization

The settings and tray menu support **English and Simplified Chinese**. Open **OSD Settings → Interface language** and choose **System / 简体中文 / English**. Changes apply immediately and are saved when settings close. Chinese Windows UI cultures use Simplified Chinese by default; all others fall back to English. The OSD keeps MAIN / ALT / DIM / MUTE / dB unchanged.

For another language, copy the AI prompt in [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md). It explains how to add a resource file, preserve hardware behavior, build a portable version and check the result. No AI account or translation service is needed to use the app.

### Quick AI translation prompt

```text
Translate this repository into [YOUR LANGUAGE]. Follow docs/LOCALIZATION.md, add a language resource based on Strings.en.xaml, preserve existing languages and hardware behavior, check layout and language switching, then build a portable Windows ZIP with matching source. Report actual tests separately from unverified behavior. Do not redesign the app or publish without my instruction.
```

## License and attribution — review pending

The WPF application code's copyright owner needs to **choose and publish a project license**. No license is granted by uploading a repository without a LICENSE file. **This is a public experimental preview, not yet a fully licensed open-source release.**

The `bridge/src/` code is derived from [alexanderdalesio/focusrite-control-2-api](https://github.com/alexanderdalesio/focusrite-control-2-api), which is **MIT** licensed. Its original MIT copyright notice is preserved in [`third_party/focusrite-control-2-api-LICENSE.txt`](third_party/focusrite-control-2-api-LICENSE.txt).

The `aes70` npm dependency is reported as **GPL-2.0**. Bundling and redistributing it introduces license compliance obligations; see [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md). The maintainer should check project-level license compatibility and complete the license review before publicly distributing binaries.

## Contributing

Issues, bug reports and translations are welcome. When reporting bugs, **do not** post FC2 private keys, pairing details, device serial numbers or unredacted logs.
