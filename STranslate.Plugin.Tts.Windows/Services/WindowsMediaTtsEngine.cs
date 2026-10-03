using STranslate.Plugin.Tts.Windows.Models;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace STranslate.Plugin.Tts.Windows.Services;

internal sealed class WindowsMediaTtsEngine : ITtsEngine
{
    private readonly SpeechSynthesizer _synthesizer = new();

    public IReadOnlyList<TtsVoice> GetVoices() => SpeechSynthesizer.AllVoices
        .Select(voice => new TtsVoice(voice.Id, voice.DisplayName, voice.Language))
        .OrderBy(voice => voice.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceId,
        double speed,
        int volume,
        CancellationToken cancellationToken)
    {
        var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(item => item.Id == voiceId);
        if (voice is null)
            throw new InvalidOperationException($"The Windows Media voice '{voiceId}' is unavailable.");

        _synthesizer.Voice = voice;
        _synthesizer.Options.SpeakingRate = Math.Clamp(speed, 0.5, 2.0);
        _synthesizer.Options.AudioVolume = Math.Clamp(volume, 0, 100) / 100d;

        using var speechStream = await _synthesizer
            .SynthesizeTextToStreamAsync(text)
            .AsTask(cancellationToken);
        if (speechStream.Size > uint.MaxValue)
            throw new InvalidOperationException("The synthesized audio is too large.");

        using var input = speechStream.GetInputStreamAt(0);
        using var reader = new DataReader(input);
        await reader.LoadAsync((uint)speechStream.Size).AsTask(cancellationToken);
        var audio = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(audio);
        return audio;
    }

    public void Dispose() => _synthesizer.Dispose();
}
