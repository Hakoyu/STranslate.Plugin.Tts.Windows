namespace STranslate.Plugin.Tts.Windows.Models;

public sealed record TtsVoice(string Id, string Name, string Language)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Language)
        ? Name
        : $"{Name} ({Language})";
}
