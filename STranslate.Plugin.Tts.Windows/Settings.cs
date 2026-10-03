namespace STranslate.Plugin.Tts.Windows;

public sealed class Settings
{
    public TtsEngineKind Engine { get; set; } = TtsEngineKind.WindowsMedia;
    public string SystemSpeechVoiceId { get; set; } = string.Empty;
    public string WindowsMediaVoiceId { get; set; } = string.Empty;
    public double Speed { get; set; } = 1.0;
    public int Volume { get; set; } = 100;

    public string GetVoiceId(TtsEngineKind engine) => engine switch
    {
        TtsEngineKind.SystemSpeech => SystemSpeechVoiceId,
        TtsEngineKind.WindowsMedia => WindowsMediaVoiceId,
        _ => string.Empty
    };

    public void SetVoiceId(TtsEngineKind engine, string voiceId)
    {
        if (engine == TtsEngineKind.SystemSpeech)
            SystemSpeechVoiceId = voiceId;
        else if (engine == TtsEngineKind.WindowsMedia)
            WindowsMediaVoiceId = voiceId;
    }
}
