using STranslate.Plugin.Tts.Windows.Models;

namespace STranslate.Plugin.Tts.Windows.Services;

internal interface ITtsEngine : IDisposable
{
    IReadOnlyList<TtsVoice> GetVoices();

    Task<byte[]> SynthesizeAsync(
        string text,
        string voiceId,
        double speed,
        int volume,
        CancellationToken cancellationToken);
}
