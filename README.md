# STranslate Windows Local TTS

使用 Windows 本机已安装语音的 STranslate 离线语音合成插件。文本和音频不会发送到网络。

## 功能

- 支持 `Windows.Media.SpeechSynthesis`（OneCore）
- 支持 Windows 原生 SAPI COM（经典桌面语音，无额外运行库依赖）
- 可分别保存两个引擎的音色
- 支持 0.50×–2.00× 语速和 0–100% 音量
- 支持设置页测试朗读

两个引擎拥有各自的音色列表。所选引擎或音色不可用时，插件会直接报错，不会自动切换引擎或音色。

## 安装与配置

1. 安装所需的 Windows 语言包和语音。
2. 在 STranslate 中进入 **设置** → **插件** → **安装插件**。
3. 选择 `.spkg` 文件并重启 STranslate。
4. 在 TTS 服务中添加 **Windows Local TTS**。
5. 选择语音引擎和音色，并按需调整语速与音量。

`Windows Media (OneCore)` 通常使用 Windows 设置中安装的现代语音；`SAPI COM` 直接调用 Windows 经典桌面语音，不依赖 `System.Speech.dll`。某个音色出现在一个列表中，不代表它也会出现在另一个列表中。

## 本地编译

需要 .NET 10 SDK 和 Windows 10 SDK：

```powershell
dotnet build .\STranslate.Plugin.Tts.Windows\STranslate.Plugin.Tts.Windows.csproj -c Release
```

Release 构建会在 `.artifacts/plugins/` 生成 `.spkg`。

## 许可证

[MIT](LICENSE)
