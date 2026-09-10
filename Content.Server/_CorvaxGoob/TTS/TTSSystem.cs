using System.Threading.Tasks;
using Content.Server._EinsteinEngines.Language;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Radio;
using Content.Server.Station.Systems;
using Content.Shared._CorvaxGoob;
using Content.Shared._CorvaxGoob.CCCVars;
using Content.Shared._CorvaxGoob.TTS;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxGoob.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LanguageSystem _lang = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private static readonly TimeSpan AnnouncementPostSignalDelay = TimeSpan.FromSeconds(2.25);

    private bool _isEnabled;
    private int _generation;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<RadioTransmissionFinishedEvent>(OnRadioTransmissionFinished);
        SubscribeLocalEvent<CommunicationConsoleAnnouncementEvent>(OnConsoleAnnouncement);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _generation++;
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !ClientWantsTts(args.SenderSession) ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
        {
            return;
        }

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var generation = _generation;
        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null || generation != _generation)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession), recordReplay: false);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            !args.Language.SpeechOverride.RequireSpeech ||
            !TryResolveSpeaker(uid, component, out var speaker, out var pitch))
        {
            return;
        }

        if (args.IsWhisper)
            await HandleWhisper(uid, args.Message, args.Language, speaker, pitch);
        else
            await HandleSay(uid, args.Message, args.Language, speaker, pitch);
    }

    private async Task HandleSay(EntityUid uid, string message, LanguagePrototype language, string speaker, float pitch)
    {
        var sourceNetEntity = GetNetEntity(uid);
        var understoodFilter = Filter.Empty();
        var obfuscatedFilter = Filter.Empty();
        SplitLanguageRecipients(
            Filter.Pvs(uid).Recipients,
            language,
            understoodFilter,
            obfuscatedFilter,
            ClientWantsTts);

        var generation = _generation;
        var understoodTask = understoodFilter.Recipients.Count > 0
            ? GenerateTTS(message, speaker)
            : Task.FromResult<byte[]?>(null);
        var obfuscatedTask = obfuscatedFilter.Recipients.Count > 0
            ? GenerateTTS(_lang.ObfuscateSpeech(message, language), speaker)
            : Task.FromResult<byte[]?>(null);

        await Task.WhenAll(understoodTask, obfuscatedTask);
        if (generation != _generation)
            return;

        if (understoodTask.Result is { } understoodSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(understoodSound, sourceNetEntity, pitch: pitch),
                understoodFilter,
                recordReplay: false);
        }

        if (obfuscatedTask.Result is { } obfuscatedSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(obfuscatedSound, sourceNetEntity, pitch: pitch),
                obfuscatedFilter,
                recordReplay: false);
        }
    }

    private async Task HandleWhisper(EntityUid uid, string message, LanguagePrototype language, string speaker, float pitch)
    {
        var sourceNetEntity = GetNetEntity(uid);
        var understoodFilter = Filter.Empty();
        var obfuscatedFilter = Filter.Empty();

        var xformQuery = GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(uid, out var sourceXform))
            return;

        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);
        var recipients = Filter.Pvs(uid).Recipients;
        foreach (var session in recipients)
        {
            if (!ClientWantsTts(session) ||
                session.AttachedEntity is not { } listener ||
                !xformQuery.TryGetComponent(listener, out var listenerXform))
            {
                continue;
            }

            var distance = (sourcePos - _xforms.GetWorldPosition(listenerXform, xformQuery)).Length();
            if (distance > ChatSystem.WhisperClearRange)
                continue;

            if (_lang.CanUnderstand(listener, language.ID))
                understoodFilter.AddPlayer(session);
            else
                obfuscatedFilter.AddPlayer(session);
        }

        var generation = _generation;
        var understoodTask = understoodFilter.Recipients.Count > 0
            ? GenerateTTS(message, speaker, isWhisper: true)
            : Task.FromResult<byte[]?>(null);
        var obfuscatedTask = obfuscatedFilter.Recipients.Count > 0
            ? GenerateTTS(_lang.ObfuscateSpeech(message, language), speaker, isWhisper: true)
            : Task.FromResult<byte[]?>(null);

        await Task.WhenAll(understoodTask, obfuscatedTask);
        if (generation != _generation)
            return;

        if (understoodTask.Result is { } understoodSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(understoodSound, sourceNetEntity, isWhisper: true, pitch: pitch),
                understoodFilter,
                recordReplay: false);
        }

        if (obfuscatedTask.Result is { } obfuscatedSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(obfuscatedSound, sourceNetEntity, isWhisper: true, pitch: pitch),
                obfuscatedFilter,
                recordReplay: false);
        }
    }

    private async void OnRadioTransmissionFinished(RadioTransmissionFinishedEvent ev)
    {
        if (!_isEnabled ||
            ev.Message.Length > MaxMessageChars ||
            !ev.Language.SpeechOverride.RequireSpeech ||
            !TryGetRadioChannelFlag(ev.Channel.ID, out var channelFlag) ||
            !TryComp<TTSComponent>(ev.MessageSource, out var ttsComponent) ||
            !TryResolveSpeaker(ev.MessageSource, ttsComponent, out var speaker, out var pitch))
        {
            return;
        }

        var sourceNetEntity = GetNetEntity(ev.MessageSource);
        var understoodFilter = Filter.Empty();
        var obfuscatedFilter = Filter.Empty();
        var seenSessions = new HashSet<ICommonSession>();

        foreach (var receiver in ev.Receivers)
        {
            if (!TryGetRadioListener(receiver, out var listener) ||
                listener == ev.MessageSource ||
                HasComp<GhostComponent>(listener) ||
                !TryComp(listener, out ActorComponent? actor) ||
                !seenSessions.Add(actor.PlayerSession) ||
                !ClientWantsRadioTts(actor.PlayerSession, channelFlag))
            {
                continue;
            }

            if (_lang.CanUnderstand(listener, ev.Language.ID))
                understoodFilter.AddPlayer(actor.PlayerSession);
            else
                obfuscatedFilter.AddPlayer(actor.PlayerSession);
        }

        var generation = _generation;
        var understoodTask = understoodFilter.Recipients.Count > 0
            ? GenerateTTS(ev.Message, speaker, ev.IsWhisper)
            : Task.FromResult<byte[]?>(null);
        var obfuscatedTask = obfuscatedFilter.Recipients.Count > 0
            ? GenerateTTS(_lang.ObfuscateSpeech(ev.Message, ev.Language), speaker, ev.IsWhisper)
            : Task.FromResult<byte[]?>(null);

        await Task.WhenAll(understoodTask, obfuscatedTask);
        if (generation != _generation)
            return;

        if (understoodTask.Result is { } understoodSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(understoodSound, sourceNetEntity, isWhisper: ev.IsWhisper, pitch: pitch, isRadio: true),
                understoodFilter,
                recordReplay: false);
        }

        if (obfuscatedTask.Result is { } obfuscatedSound)
        {
            RaiseNetworkEvent(
                new PlayTTSEvent(obfuscatedSound, sourceNetEntity, isWhisper: ev.IsWhisper, pitch: pitch, isRadio: true),
                obfuscatedFilter,
                recordReplay: false);
        }
    }

    private void SplitLanguageRecipients(
        IEnumerable<ICommonSession> recipients,
        LanguagePrototype language,
        Filter understoodFilter,
        Filter obfuscatedFilter,
        Func<ICommonSession, bool> canReceive)
    {
        foreach (var session in recipients)
        {
            if (!canReceive(session) ||
                session.AttachedEntity is not { } listener)
            {
                continue;
            }

            if (_lang.CanUnderstand(listener, language.ID))
                understoodFilter.AddPlayer(session);
            else
                obfuscatedFilter.AddPlayer(session);
        }
    }

    private bool TryGetRadioListener(EntityUid receiver, out EntityUid listener)
    {
        listener = receiver;
        if (HasComp<ActorComponent>(receiver))
            return true;

        if (!TryComp<HeadsetComponent>(receiver, out var headset) || !headset.IsEquipped)
            return false;

        listener = Transform(receiver).ParentUid;
        return listener.IsValid();
    }

    private bool TryResolveSpeaker(EntityUid uid, TTSComponent component, out string speaker, out float pitch)
    {
        speaker = string.Empty;
        pitch = component.Pitch;

        if (component.VoicePrototypeId is not { } voicePrototypeId)
            return false;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voicePrototypeId.Id);
        RaiseLocalEvent(uid, voiceEv);

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceEv.VoiceId, out var protoVoice))
            return false;

        speaker = protoVoice.Speaker;
        return true;
    }

    private bool ClientWantsTts(ICommonSession session)
    {
        return _netCfg.GetClientCVar(session.Channel, CCCVars.TTSVolume) > 0f;
    }

    private bool ClientWantsRadioTts(ICommonSession session, RadioChannelFlag channel)
    {
        if (!ClientWantsTts(session) ||
            _netCfg.GetClientCVar(session.Channel, CCCVars.TTSRadioVolume) <= 0f)
        {
            return false;
        }

        var filter = (RadioChannelFlag) _netCfg.GetClientCVar(session.Channel, CCCVars.TTSRadioFilter);
        return (filter & channel) != 0;
    }

    private bool ClientWantsAnnouncementTts(ICommonSession session)
    {
        return ClientWantsTts(session) &&
               _netCfg.GetClientCVar(session.Channel, CCCVars.AnnouncementsSound) > 0f;
    }

    private static bool TryGetRadioChannelFlag(string channelId, out RadioChannelFlag flag)
    {
        if (Enum.TryParse(channelId, out flag) &&
            flag != RadioChannelFlag.None &&
            flag != RadioChannelFlag.Binary)
        {
            return true;
        }

        flag = RadioChannelFlag.None;
        return false;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast;
        if (isWhisper)
            ssmlTraits = SoundTraits.PitchVerylow;

        var textSsml = ToSsmlText(textSanitized, ssmlTraits);
        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }

    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent ev)
    {
        if (!_isEnabled ||
            ev.Text.Length > _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength))
        {
            return;
        }

        var voice = TryGetAnnouncementSpeaker(ev.Sender);
        if (voice is null)
            return;

        if (ev.Component.Global)
            SendGlobalAnnouncement(ev.Text, voice, ev.Component.Sound);
        else
            SendStationAnnouncement(ev.Uid, ev.Text, voice, ev.Component.Sound);
    }

    private string? TryGetAnnouncementSpeaker(EntityUid? sender)
    {
        const string fallbackVoice = "Glados";

        if (sender is { } senderUid &&
            TryComp<TTSComponent>(senderUid, out var ttsComponent) &&
            !HasComp<MutedComponent>(senderUid) &&
            TryResolveSpeaker(senderUid, ttsComponent, out var speaker, out _))
        {
            return speaker;
        }

        return _prototypeManager.TryIndex<TTSVoicePrototype>(fallbackVoice, out var protoVoice)
            ? protoVoice.Speaker
            : null;
    }

    private void SendGlobalAnnouncement(string text, string voice, SoundSpecifier announcementSound)
    {
        SendTTS(Filter.Broadcast(), text, voice, announcementSound);
    }

    private void SendStationAnnouncement(EntityUid consoleUid, string text, string voice, SoundSpecifier announcementSound)
    {
        var station = _stationSystem.GetOwningStation(consoleUid);
        if (station is null ||
            !TryComp<StationDataComponent>(station, out var stationDataComp))
        {
            return;
        }

        SendTTS(_stationSystem.GetInStation(stationDataComp), text, voice, announcementSound);
    }

    private async void SendTTS(Filter recipients, string text, string voice, SoundSpecifier announcementSound)
    {
        var filter = Filter.Empty();
        foreach (var session in recipients.Recipients)
        {
            if (ClientWantsAnnouncementTts(session))
                filter.AddPlayer(session);
        }

        if (filter.Recipients.Count == 0)
            return;

        var generation = _generation;
        var sendAt = _timing.CurTime + _audio.GetAudioLength(_audio.ResolveSound(announcementSound)) + AnnouncementPostSignalDelay;
        var soundData = await GenerateTTS(text, voice);
        if (soundData is null || generation != _generation)
            return;

        var delay = sendAt - _timing.CurTime;
        if (delay <= TimeSpan.Zero)
        {
            RaiseNetworkEvent(new TTSAnnouncedEvent(soundData), filter, recordReplay: false);
            return;
        }

        Timer.Spawn(delay, () =>
        {
            if (generation != _generation)
                return;

            RaiseNetworkEvent(new TTSAnnouncedEvent(soundData), filter, recordReplay: false);
        });
    }

    public void SendTTSAdminAnnouncement(string text, string voice, string announcementPath = ChatSystem.CentComAnnouncementSound)
    {
        if (!_isEnabled ||
            text.Length > _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength) ||
            voice == "None" ||
            voice == "")
        {
            return;
        }

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voice, out var protoVoice))
            return;

        SendTTS(Filter.Broadcast(), text, protoVoice.Speaker, new SoundPathSpecifier(announcementPath));
    }
}
