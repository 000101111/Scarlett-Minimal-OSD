# Community translation / 社区翻译

[English README](../README.md) · [中文说明](../README.zh-CN.md)

The app includes English and Simplified Chinese. Select **OSD Settings → Interface language**. The default follows Windows: Chinese UI cultures use Simplified Chinese; other cultures use English. Switching applies immediately and is saved when settings close. MAIN, ALT, DIM, MUTE and dB stay unchanged.

软件内置英文和简体中文，在「OSD 设置 → 界面语言」切换。默认跟随 Windows：中文系统使用简体中文，其他系统使用英文。切换立即生效，关闭设置时保存。

Want another language? Clone this repository and paste this prompt into your AI coding assistant. Replace `[TARGET LANGUAGE]` and `[LOCALE CODE]` (for example, German / de-DE). Check the project and dependency license notices before redistributing a translated build.

想手搓自己的语言版本？克隆仓库后，把下面的 Prompt 交给 AI 编程助手，替换目标语言和语言代码。分发翻译版前请查看项目及依赖的许可证说明。

```text
Localize Scarlett Minimal OSD into [TARGET LANGUAGE] ([LOCALE CODE]).

1. Read README.md, README.zh-CN.md, docs/LOCALIZATION.md and the existing implementation before editing.
2. Copy src/Localization/Strings.en.xaml to Strings.[LOCALE CODE].xaml. Translate every string value naturally; preserve x:Key names, XML escaping and formatting placeholders such as {0:0.0}.
3. Keep English as the fallback. Add the locale to Localization.Normalize, the settings language selector and its selection mapping; handle the relevant Windows UI culture explicitly. Preserve auto, en and zh-CN behavior and persisted settings.
4. Use the same resources for settings text and Windows tray menu labels. Leave MAIN, ALT, DIM, MUTE, dB and product names unchanged. Keep developer logs and protocol values unchanged.
5. Preserve the minimalist UI. Allow wrapping or modest layout fixes if translated text is longer. Do not redesign the OSD or add unrelated features.
6. Do not change AES70 communication, hardware gain limits, MAIN/ALT behavior, FC2 show/hide, audio routing, pairing, drivers or firmware. Do not copy private keys, device IDs or local settings into the repository.
7. Build the Windows Release version with build.ps1. Check the settings, tray labels, system default, immediate switching, persistence after restart, English fallback and text overflow on Windows. Separate actual tests from checks you could not run.
8. Deliver a compiled portable ZIP with the matching source and third-party notices. Do not require the end user to compile. Provide a concise summary and a patch/PR if appropriate; do not publish without the owner's instruction.
```

## Resource structure / 文件结构

- `src/Localization/Strings.en.xaml`: English fallback / 英文回退资源。
- `src/Localization/Strings.zh-CN.xaml`: Simplified Chinese / 简体中文。
- `src/Localization.cs`: locale resolution, resource loading and tray text lookup / 语言选择和资源加载。
- `src/SettingsWindow.xaml` and `.xaml.cs`: language selector and live switching / 语言选项和即时切换。
- `src/AppSettings.cs`: saved `Language` preference (`auto`, `en`, `zh-CN`) / 保存语言偏好。

The runtime loads WPF resources packaged in the application. It does not download translations or run AI services.

语言资源随程序打包，不联网下载翻译，也不调用 AI 服务。
