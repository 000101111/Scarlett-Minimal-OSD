# Scarlett Minimal OSD

**简体中文 · [English](README.md)**

为 **Focusrite Scarlett 16i16 第四代**制作的极简 Windows 监听音量 OSD 和轻量控制工具，通过 Focusrite Control 2（FC2）读取、控制设备。

**内置中文 + English** · [下载编译好的便携预览版](https://github.com/000101111/Scarlett-Minimal-OSD/releases/tag/v0.1.1-alpha) · [用 AI 手搓你的语言版本：翻译 Prompt](docs/LOCALIZATION.md)

想用其他语言？把上面的翻译 Prompt 交给 AI 编程助手即可开始。界面文字已集中到语言资源文件，方便翻译。

> 非官方社区项目，与 Focusrite 无隶属关系，也未经其背书。Focusrite 和 Scarlett 商标属于其所有者。Windows Release 自动编译已通过；原贡献者报告在自己的设备上正常使用，这里未独立完成硬件实测，其他 Scarlett 型号尚未验证。当前为实验性预览版。

## 界面和操作

280 × 100 的纯黑直角小框，仅显示 **MAIN / ALT、设备实际监听增益（dB）**，以及启用时的 **MUTE / DIM**。选中组显示红色，音量数字为白色。默认位于屏幕右侧居中，短暂停留后自动隐藏。

| 操作 | 功能 |
| --- | --- |
| 转动实体监听旋钮 | 同步显示设备报告的监听增益 |
| 按下实体 ALT 按钮 | 同步 MAIN / ALT 状态 |
| 点击 OSD 的 MAIN / ALT | 请求切换实际硬件监听组 |
| 在 Windows 托盘区域滚动滚轮 | 调节监听增益；请注意下方已知限制 |
| 双击本程序托盘图标 | 显示 / 隐藏 FC2 主窗口 |
| 点击 OSD 空白处或音量数字 | 显示 FC2 主窗口 |
| 右键托盘图标 | FC2 显示 / 隐藏、OSD 设置、退出 |
| 打开设置 | 调节透明度、显示时长、滚轮步进、增益上限、FC2 行为及界面语言 |

隐藏 FC2 只隐藏其窗口，进程和原厂设备通信继续运行。

## 下载和运行

1. 在 [Release 页面](https://github.com/000101111/Scarlett-Minimal-OSD/releases/tag/v0.1.1-alpha) 下载 `Scarlett-Minimal-OSD-Windows-x64-Preview.zip`。这是含 EXE 的便携包，不需要自己编译。
2. 解压到可写文件夹，让 `ScarlettMonitorOSD.exe`、`node.exe` 和 `bridge/` 保持在一起。
3. 安装官方 Focusrite Control 2。如系统提示缺少运行环境，请安装 .NET 8 Desktop Runtime。
4. 启动 FC2，再运行 `ScarlettMonitorOSD.exe`。先从低监听音量开始测试。
5. 在「OSD 设置 → 界面语言」选择「跟随系统 / 简体中文 / English」。立即生效，关闭设置时保存。

运行要求：Windows 10/11 x64、兼容的 Scarlett、FC2、.NET 8 Desktop Runtime。便携包附带 Node.js。程序仍依赖 `bridge/` 和运行环境，不是完全独立的单文件 EXE。

**首次配对限制：**新电脑或新的 Windows 用户账户需要 FC2 API 授权身份。当前 OSD 桥接没有完整的 Windows 自动配对引导，可能需要上游 API 工具手动配对；内置发现辅助程序调用 Unix `lsof`。如果显示离线，请确认 FC2 正在运行，并且当前用户的 `%APPDATA%\focusrite-control-2-api` 配置有效。不要把其中的私钥或身份文件公开。

## 语言与 AI 翻译

默认跟随 Windows 界面语言：中文使用简体中文，其他语言回退到英文。设置菜单和托盘菜单支持即时切换。MAIN、ALT、MUTE、DIM、dB 保持原样。

其他语言可以使用 [社区翻译 Prompt](docs/LOCALIZATION.md)，让 AI 编程助手添加语言资源、检查排版并编译便携包。使用软件本身不需要 AI 账户，也不调用在线翻译服务。

### 可直接复制给 AI 的简版 Prompt

```text
请把这个项目翻译成【目标语言】。按照 docs/LOCALIZATION.md，以 Strings.en.xaml 为基础添加语言资源，保留现有语言和硬件控制行为，检查排版、即时切换与语言保存，然后编译含匹配源码的 Windows 便携 ZIP。区分实际测试和未验证功能，不重做界面，不擅自发布。
```

## 已知限制

- 当前滚轮监听覆盖 **Windows 托盘区域**，并非只识别本程序图标。在其他托盘图标上滚动也可能改变增益；修复前请避免使用该操作。
- 界面默认增益上限为 0 dB，但 Node 桥接内部上限更宽松，不应向不可信程序开放其标准输入或实验 API。
- 首次配对、新用户环境、其他型号、设备重连及异常恢复仍需进一步实测。
- FC2 窗口识别依赖标题和窗口类名，原厂软件更新可能影响显示 / 隐藏。
- 当前不包含 ASIO DSP、EQ、虚拟环绕或音频 Peak 表，不修改原厂驱动和固件。

## 从源码构建

Windows 安装 **.NET 8 SDK** 和 **Node.js 20+** 后运行 `build.cmd` 或 `build.ps1`。脚本安装 npm 依赖，输出到 `dist/ScarlettMonitorOSD`。仓库包含 Windows 自动构建流程。

## 许可证与来源

原始 WPF 代码的项目许可证尚待版权方选择。公开源码不代表已经授予完整开源许可；本项目当前不应被描述为许可证已完善的开源发行版。

`bridge/src/` 源自 [alexanderdalesio/focusrite-control-2-api](https://github.com/alexanderdalesio/focusrite-control-2-api)，其 MIT 声明保留在 `third_party/`。`aes70` npm 依赖标注 GPL-2.0，其他依赖也有各自条款；分发前应查看 [第三方说明](THIRD_PARTY_NOTICES.md) 并完成许可证审查。

欢迎反馈、翻译和 PR。反馈中请勿公开配对私钥、设备序列号、私人配置或未脱敏日志。
