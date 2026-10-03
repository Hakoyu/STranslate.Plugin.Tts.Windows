using STranslate.Plugin.Tts.Windows.Models;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace STranslate.Plugin.Tts.Windows.Services;

internal sealed class SapiTtsEngine : ITtsEngine
{
    private const int SpeechVoiceSpeakFlagsAsync = 1;
    private const int SpeechVoiceSpeakFlagsPurgeBeforeSpeak = 2;
    private const int SpeechStreamFileModeCreateForWrite = 3;

    public IReadOnlyList<TtsVoice> GetVoices()
    {
        dynamic? voice = null;
        dynamic? tokens = null;
        try
        {
            voice = CreateComObject("SAPI.SpVoice");
            tokens = voice.GetVoices();
            var voices = new List<TtsVoice>((int)tokens.Count);
            for (var index = 0; index < (int)tokens.Count; index++)
            {
                dynamic? token = null;
                try
                {
                    token = tokens.Item(index);
                    var id = (string)token.Id;
                    var name = (string)token.GetDescription(0);
                    voices.Add(new TtsVoice(id, name, GetLanguage(token)));
                }
                finally
                {
                    ReleaseComObject(token);
                }
            }

            return voices
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        finally
        {
            ReleaseComObject(tokens);
            ReleaseComObject(voice);
        }
    }

    public Task<byte[]> SynthesizeAsync(
        string text,
        string voiceId,
        double speed,
        int volume,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outputPath = Path.Combine(Path.GetTempPath(), $"stranslate-sapi-{Guid.NewGuid():N}.wav");
        dynamic? voice = null;
        dynamic? tokens = null;
        dynamic? selectedToken = null;
        dynamic? outputStream = null;
        try
        {
            voice = CreateComObject("SAPI.SpVoice");
            tokens = voice.GetVoices();
            for (var index = 0; index < (int)tokens.Count; index++)
            {
                dynamic token = tokens.Item(index);
                if ((string)token.Id == voiceId)
                {
                    selectedToken = token;
                    break;
                }

                ReleaseComObject(token);
            }

            if (selectedToken is null)
                throw new InvalidOperationException($"The SAPI voice '{voiceId}' is unavailable.");

            voice.Voice = selectedToken;
            voice.Rate = (int)Math.Round(Math.Log2(Math.Clamp(speed, 0.5, 2.0)) * 10);
            voice.Volume = Math.Clamp(volume, 0, 100);

            outputStream = CreateComObject("SAPI.SpFileStream");
            outputStream.Open(outputPath, SpeechStreamFileModeCreateForWrite, false);
            voice.AudioOutputStream = outputStream;
            voice.Speak(text, SpeechVoiceSpeakFlagsAsync);

            while (!(bool)voice.WaitUntilDone(50))
            {
                if (!cancellationToken.IsCancellationRequested)
                    continue;

                voice.Speak(string.Empty, SpeechVoiceSpeakFlagsPurgeBeforeSpeak);
                cancellationToken.ThrowIfCancellationRequested();
            }

            outputStream.Close();
            ReleaseComObject(outputStream);
            outputStream = null;

            cancellationToken.ThrowIfCancellationRequested();
            return File.ReadAllBytes(outputPath);
        }
        finally
        {
            if (outputStream is not null)
            {
                try
                {
                    outputStream.Close();
                }
                catch (COMException)
                {
                }
            }

            ReleaseComObject(outputStream);
            ReleaseComObject(selectedToken);
            ReleaseComObject(tokens);
            ReleaseComObject(voice);
            try
            {
                File.Delete(outputPath);
            }
            catch (IOException)
            {
            }
        }
    }, cancellationToken);

    private static dynamic CreateComObject(string progId)
    {
        var type = Type.GetTypeFromProgID(progId, throwOnError: false)
            ?? throw new PlatformNotSupportedException($"Windows Speech API component '{progId}' is unavailable.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not create Windows Speech API component '{progId}'.");
    }

    private static string GetLanguage(dynamic token)
    {
        try
        {
            var language = ((string)token.GetAttribute("Language"))
                .Split(';', StringSplitOptions.RemoveEmptyEntries)[0];
            var lcid = int.Parse(language, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return CultureInfo.GetCultureInfo(lcid).Name;
        }
        catch (Exception ex) when (ex is COMException or FormatException or CultureNotFoundException)
        {
            return string.Empty;
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }

    public void Dispose()
    {
    }
}
