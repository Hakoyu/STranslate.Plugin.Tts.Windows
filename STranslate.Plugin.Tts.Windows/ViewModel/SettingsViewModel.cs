using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using STranslate.Plugin.Tts.Windows.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace STranslate.Plugin.Tts.Windows.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;
    private readonly Main _main;
    private bool _isUpdating;

    public SettingsViewModel(IPluginContext context, Settings settings, Main main)
    {
        _context = context;
        _settings = settings;
        _main = main;

        Engines =
        [
            new(TtsEngineKind.WindowsMedia, context.GetTranslation("STranslate_Plugin_Tts_Windows_Engine_WindowsMedia")),
            new(TtsEngineKind.SystemSpeech, context.GetTranslation("STranslate_Plugin_Tts_Windows_Engine_SystemSpeech"))
        ];

        _isUpdating = true;
        Engine = settings.Engine;
        Speed = Math.Clamp(settings.Speed, 0.5, 2.0);
        Volume = Math.Clamp(settings.Volume, 0, 100);
        _isUpdating = false;

        PropertyChanged += OnPropertyChanged;
        RefreshVoicesCore();
    }

    public IReadOnlyList<TtsEngineOption> Engines { get; }
    public ObservableCollection<TtsVoice> Voices { get; } = [];

    [ObservableProperty] public partial TtsEngineKind Engine { get; set; }
    [ObservableProperty] public partial string? VoiceId { get; set; }
    [ObservableProperty] public partial double Speed { get; set; } = 1.0;
    [ObservableProperty] public partial int Volume { get; set; } = 100;
    [ObservableProperty] public partial string Status { get; set; } = string.Empty;

    partial void OnEngineChanged(TtsEngineKind value)
    {
        if (_isUpdating)
            return;

        _settings.Engine = value;
        _context.SaveSettingStorage<Settings>();
        RefreshVoicesCore();
    }

    [RelayCommand]
    private void RefreshVoices() => RefreshVoicesCore(showSuccess: true);

    [RelayCommand]
    private async Task TestAsync()
    {
        try
        {
            Status = string.Empty;
            await _main.TestAsync();
            Status = _context.GetTranslation("STranslate_Plugin_Tts_Windows_TestSuccess");
            _context.Snackbar.ShowSuccess(Status);
        }
        catch (Exception ex)
        {
            Status = _context.GetTranslation("STranslate_Plugin_Tts_Windows_TestFailure");
            _context.Logger.LogError(ex, Status);
            _context.Snackbar.ShowError($"{Status}: {ex.Message}");
        }
    }

    private void RefreshVoicesCore(bool showSuccess = false)
    {
        _isUpdating = true;
        try
        {
            var voices = _main.GetVoices(Engine);
            Voices.Clear();
            foreach (var voice in voices)
                Voices.Add(voice);

            var savedVoiceId = _settings.GetVoiceId(Engine);
            VoiceId = Voices.Any(voice => voice.Id == savedVoiceId) ? savedVoiceId : null;
            if (string.IsNullOrWhiteSpace(savedVoiceId) && Voices.Count > 0)
            {
                VoiceId = Voices[0].Id;
                _settings.SetVoiceId(Engine, VoiceId);
                _context.SaveSettingStorage<Settings>();
            }

            Status = string.Format(
                _context.GetTranslation("STranslate_Plugin_Tts_Windows_VoicesLoaded"),
                Voices.Count);
            if (showSuccess)
                _context.Snackbar.ShowSuccess(Status);
        }
        catch (Exception ex)
        {
            Voices.Clear();
            VoiceId = null;
            Status = _context.GetTranslation("STranslate_Plugin_Tts_Windows_VoicesLoadFailure");
            _context.Logger.LogError(ex, Status);
            _context.Snackbar.ShowError($"{Status}: {ex.Message}");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdating)
            return;

        switch (e.PropertyName)
        {
            case nameof(VoiceId):
                _settings.SetVoiceId(Engine, VoiceId ?? string.Empty);
                break;
            case nameof(Speed):
                _settings.Speed = Math.Round(Math.Clamp(Speed, 0.5, 2.0), 2);
                break;
            case nameof(Volume):
                _settings.Volume = Math.Clamp(Volume, 0, 100);
                break;
            default:
                return;
        }

        _context.SaveSettingStorage<Settings>();
    }

    public void Dispose() => PropertyChanged -= OnPropertyChanged;
}
