# Translation / 翻译

The app currently uses Chinese labels for settings and menu items. The MAIN, ALT, DIM, MUTE and dB labels should normally remain unchanged.

Use this prompt with a coding assistant:

```text
Localize Scarlett Minimal OSD to [TARGET LANGUAGE].
Find all user-visible Chinese strings in src/*.xaml and src/*.cs,
especially tray menus, SettingsWindow, labels and errors.
Preserve the 280x100 minimal OSD, MAIN/ALT/dB hardware labels,
and all device-control and Focusrite Control 2 communication logic.
Prefer .resx resource files for future localization, with English
fallback. Do not modify gain safety limits or device pairing.
Build on Windows and verify layout does not overflow.
Return only language-resource and necessary UI wiring changes.
```

For pull requests, state the target language and show screenshots of the settings page; do not commit any personal `settings.json`, API configuration, pairing identity or logs.
