using STranslate.Plugin.Tts.Windows.Models;
using STranslate.Plugin.Tts.Windows.Services;
using STranslate.Plugin.Tts.Windows.View;
using STranslate.Plugin.Tts.Windows.ViewModel;
using System.Windows.Controls;

namespace STranslate.Plugin.Tts.Windows;

public sealed class Main : ITtsPlugin
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    public void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
        Settings.Speed = Math.Clamp(Settings.Speed, 0.5, 2.0);
        Settings.Volume = Math.Clamp(Settings.Volume, 0, 100);
    }

    public Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings, this);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    public async Task PlayAudioAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var audio = await SynthesizeAsync(text, cancellationToken);
        await Context.AudioPlayer.PlayAsync(audio, cancellationToken);
    }

    internal IReadOnlyList<TtsVoice> GetVoices(TtsEngineKind engine)
    {
        using var ttsEngine = CreateEngine(engine);
        return ttsEngine.GetVoices();
    }

    internal Task TestAsync(CancellationToken cancellationToken = default) =>
        PlayAudioAsync(Context.GetTranslation("STranslate_Plugin_Tts_Windows_TestText"), cancellationToken);

    private async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        using var engine = CreateEngine(Settings.Engine);
        var voices = engine.GetVoices();
        if (voices.Count == 0)
            throw new InvalidOperationException(Context.GetTranslation("STranslate_Plugin_Tts_Windows_NoVoices"));

        var voiceId = Settings.GetVoiceId(Settings.Engine);
        if (string.IsNullOrWhiteSpace(voiceId))
            throw new InvalidOperationException(Context.GetTranslation("STranslate_Plugin_Tts_Windows_VoiceRequired"));
        if (!voices.Any(voice => voice.Id == voiceId))
        {
            throw new InvalidOperationException(string.Format(
                Context.GetTranslation("STranslate_Plugin_Tts_Windows_VoiceUnavailable"),
                voiceId));
        }

        var audio = await engine.SynthesizeAsync(
            text,
            voiceId,
            Settings.Speed,
            Settings.Volume,
            cancellationToken);
        if (audio.Length == 0)
            throw new InvalidOperationException(Context.GetTranslation("STranslate_Plugin_Tts_Windows_EmptyAudio"));

        return audio;
    }

    private static ITtsEngine CreateEngine(TtsEngineKind engine) => engine switch
    {
        TtsEngineKind.SystemSpeech => new SapiTtsEngine(),
        TtsEngineKind.WindowsMedia => new WindowsMediaTtsEngine(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine), engine, null)
    };

    public void Dispose() => _viewModel?.Dispose();
}
