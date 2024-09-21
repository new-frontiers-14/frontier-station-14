using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.UserInterface; // Nuclear-14
using Content.Shared._NC.Radio; // Nuclear-14
using Robust.Server.GameObjects; // Nuclear-14
using Robust.Shared.Prototypes;
using Content.Shared.Access.Systems; // Frontier

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles radio speakers and microphones (which together form a hand-held radio).
/// </summary>
public sealed class RadioDeviceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!; // Frontier: access

    // Used to prevent a shitter from using a bunch of radios to spam chat.
    private HashSet<(string, EntityUid)> _recentlySent = new();

    // Frontier: minimum, maximum radio frequencies
    private const int MinRadioFrequency = 1000;
    private const int MaxRadioFrequency = 3000;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioMicrophoneComponent, ComponentInit>(OnMicrophoneInit);
        SubscribeLocalEvent<RadioMicrophoneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioMicrophoneComponent, ActivateInWorldEvent>(OnActivateMicrophone);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<RadioSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<RadioSpeakerComponent, ActivateInWorldEvent>(OnActivateSpeaker);
        SubscribeLocalEvent<RadioSpeakerComponent, RadioReceiveEvent>(OnReceiveRadio);

        SubscribeLocalEvent<IntercomComponent, EncryptionChannelsChangedEvent>(OnIntercomEncryptionChannelsChanged);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomMicMessage>(OnToggleIntercomMic);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomSpeakerMessage>(OnToggleIntercomSpeaker);
        SubscribeLocalEvent<IntercomComponent, SelectIntercomChannelMessage>(OnSelectIntercomChannel);

        // Nuclear-14-Start
        SubscribeLocalEvent<RadioMicrophoneComponent, BeforeActivatableUIOpenEvent>(OnBeforeHandheldRadioUiOpen);
        SubscribeLocalEvent<RadioMicrophoneComponent, ToggleHandheldRadioMicMessage>(OnToggleHandheldRadioMic);
        SubscribeLocalEvent<RadioMicrophoneComponent, ToggleHandheldRadioSpeakerMessage>(OnToggleHandheldRadioSpeaker);
        SubscribeLocalEvent<RadioMicrophoneComponent, SelectHandheldRadioFrequencyMessage>(OnChangeHandheldRadioFrequency);
        // Nuclear-14-End

        SubscribeLocalEvent<IntercomComponent, MapInitEvent>(OnMapInit); // Frontier
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }


    #region Component Init
    private void OnMicrophoneInit(EntityUid uid, RadioMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    private void OnSpeakerInit(EntityUid uid, RadioSpeakerComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
        else
            RemCompDeferred<ActiveRadioComponent>(uid);
    }
    #endregion

    #region Toggling
    private void OnActivateMicrophone(EntityUid uid, RadioMicrophoneComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!component.ToggleOnInteract)
            return;

        ToggleRadioMicrophone(uid, args.User, args.Handled, component);
        args.Handled = true;
    }

    private void OnActivateSpeaker(EntityUid uid, RadioSpeakerComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (!component.ToggleOnInteract)
            return;

        ToggleRadioSpeaker(uid, args.User, args.Handled, component);
        args.Handled = true;
    }

    public void ToggleRadioMicrophone(EntityUid uid, EntityUid user, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetMicrophoneEnabled(uid, user, !component.Enabled, quiet, component);
    }

    private void OnPowerChanged(EntityUid uid, RadioMicrophoneComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;
        SetMicrophoneEnabled(uid, null, false,  true, component);
    }

    public void SetMicrophoneEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.PowerRequired && !this.IsPowered(uid, EntityManager))
            return;

        component.Enabled = enabled;

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Broadcasting, component.Enabled);
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    public void ToggleRadioSpeaker(EntityUid uid, EntityUid user, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetSpeakerEnabled(uid, user, !component.Enabled, quiet, component);
    }

    public void SetSpeakerEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Enabled = enabled;

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Speaker, component.Enabled);
        if (component.Enabled)
            EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
        else
            RemCompDeferred<ActiveRadioComponent>(uid);
    }
    #endregion

    private void OnExamine(EntityUid uid, RadioMicrophoneComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine", ("frequency", /*Nuclear-14-start*/ component.Frequency /*Nuclear-14-end*/)));
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void OnListen(EntityUid uid, RadioMicrophoneComponent component, ListenEvent args)
    {
        if (HasComp<RadioSpeakerComponent>(args.Source))
            return; // no feedback loops please.

        if (_recentlySent.Add((args.Message, args.Source)))
            _radio.SendRadioMessage(args.Source, args.Message, _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel), uid, /*Nuclear-14-start*/ frequency: component.Frequency /*Nuclear-14-end*/);
    }

    private void OnAttemptListen(EntityUid uid, RadioMicrophoneComponent component, ListenAttemptEvent args)
    {
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager)
            || component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0))
        {
            args.Cancel();
        }
    }

    private void OnReceiveRadio(EntityUid uid, RadioSpeakerComponent component, ref RadioReceiveEvent args)
    {
        if (uid == args.RadioSource)
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(uid)),
            ("originalName", nameEv.Name));

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chat.TrySendInGameICMessage(uid, args.Message, component.OutputChatType, ChatTransmitRange.GhostRangeLimitNoAdminCheck, nameOverride: name, checkRadioPrefix: false); // Frontier: GhostRangeLimit<GhostRangeLimitNoAdminCheck, InGameICChatType.Whisper<component.OutputChatType
    }

    private void OnIntercomEncryptionChannelsChanged(Entity<IntercomComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        ent.Comp.SupportedChannels = args.Component.Channels.Select(p => new ProtoId<RadioChannelPrototype>(p)).ToList();

        var channel = args.Component.DefaultChannel;
        if (ent.Comp.CurrentChannel != null && ent.Comp.SupportedChannels.Contains(ent.Comp.CurrentChannel.Value))
            channel = ent.Comp.CurrentChannel;

        SetIntercomChannel(ent, channel);
    }

    private void OnToggleIntercomMic(Entity<IntercomComponent> ent, ref ToggleIntercomMicMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;
        if (!_access.IsAllowed(args.Actor, ent.Owner)) // Frontier
            return; // Frontier

        SetMicrophoneEnabled(ent, args.Actor, args.Enabled, true);
        ent.Comp.MicrophoneEnabled = args.Enabled;
        Dirty(ent);
    }

    private void OnToggleIntercomSpeaker(Entity<IntercomComponent> ent, ref ToggleIntercomSpeakerMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;
        if (!_access.IsAllowed(args.Actor, ent.Owner)) // Frontier
            return; // Frontier

        SetSpeakerEnabled(ent, args.Actor, args.Enabled, true);
        ent.Comp.SpeakerEnabled = args.Enabled;
        Dirty(ent);
    }

    private void OnSelectIntercomChannel(Entity<IntercomComponent> ent, ref SelectIntercomChannelMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;
        if (!_access.IsAllowed(args.Actor, ent.Owner)) // Frontier
            return; // Frontier

        if (!_protoMan.TryIndex<RadioChannelPrototype>(args.Channel, out var channel) || !ent.Comp.SupportedChannels.Contains(args.Channel)) // Nuclear-14: add channel
            return;

        SetIntercomChannel(ent, args.Channel);
    }

    private void SetIntercomChannel(Entity<IntercomComponent> ent, ProtoId<RadioChannelPrototype>? channel)
    {
        ent.Comp.CurrentChannel = channel;

        if (channel == null)
        {
            SetSpeakerEnabled(ent, null, false);
            SetMicrophoneEnabled(ent, null, false);
            ent.Comp.MicrophoneEnabled = false;
            ent.Comp.SpeakerEnabled = false;
            Dirty(ent);
            return;
        }

        if (TryComp<RadioMicrophoneComponent>(ent, out var mic))
        {
            mic.BroadcastChannel = channel;
            if(_protoMan.TryIndex<RadioChannelPrototype>(channel, out var channelProto)) // Frontier
                mic.Frequency = _radio.GetFrequency(ent, channelProto); // Frontier
        }
        if (TryComp<RadioSpeakerComponent>(ent, out var speaker))
            speaker.Channels = new(){ channel };
        Dirty(ent);
    }

    // Nuclear-14-Start
    #region Handheld Radio

    private void OnBeforeHandheldRadioUiOpen(Entity<RadioMicrophoneComponent> microphone, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateHandheldRadioUi(microphone);
    }

    private void OnToggleHandheldRadioMic(Entity<RadioMicrophoneComponent> microphone, ref ToggleHandheldRadioMicMessage args)
    {
        if (!args.Actor.Valid)
            return;

        SetMicrophoneEnabled(microphone, args.Actor, args.Enabled, true);
        UpdateHandheldRadioUi(microphone);
    }

    private void OnToggleHandheldRadioSpeaker(Entity<RadioMicrophoneComponent> microphone, ref ToggleHandheldRadioSpeakerMessage args)
    {
        if (!args.Actor.Valid)
            return;

        SetSpeakerEnabled(microphone, args.Actor, args.Enabled, true);
        UpdateHandheldRadioUi(microphone);
    }

    private void OnChangeHandheldRadioFrequency(Entity<RadioMicrophoneComponent> microphone, ref SelectHandheldRadioFrequencyMessage args)
    {
        if (!args.Actor.Valid)
            return;

        // Update frequency if valid and within range.
        if (args.Frequency >= MinRadioFrequency && args.Frequency <= MaxRadioFrequency)
            microphone.Comp.Frequency = args.Frequency;
        // Update UI with current frequency.
        UpdateHandheldRadioUi(microphone);
    }

    private void UpdateHandheldRadioUi(Entity<RadioMicrophoneComponent> radio)
    {
        var speakerComp = CompOrNull<RadioSpeakerComponent>(radio);
        var frequency = radio.Comp.Frequency;

        var micEnabled = radio.Comp.Enabled;
        var speakerEnabled = speakerComp?.Enabled ?? false;
        var state = new HandheldRadioBoundUIState(micEnabled, speakerEnabled, frequency);
        if (TryComp<UserInterfaceComponent>(radio, out var uiComp))
            _ui.SetUiState((radio.Owner, uiComp), HandheldRadioUiKey.Key, state); // Frontier: TrySetUiState<SetUiState
    }

    #endregion
    // Nuclear-14-End

    // Frontier: init intercom with map
    private void OnMapInit(EntityUid uid, IntercomComponent ent, MapInitEvent args)
    {
        // Set initial frequency (must be done regardless of power/enabled)
        if (ent.CurrentChannel != null &&
                _protoMan.TryIndex(ent.CurrentChannel, out var channel) &&
                TryComp(uid, out RadioMicrophoneComponent? mic))
        {
            mic.Frequency = channel.Frequency;
        }
        if (ent.StartSpeakerOnMapInit)
        {
            SetSpeakerEnabled(uid, null, true);
            ent.SpeakerEnabled = true;
            _appearance.SetData(uid, RadioDeviceVisuals.Speaker, true);
        }
        if (ent.StartMicrophoneOnMapInit)
        {
            SetMicrophoneEnabled(uid, null, true);
            ent.MicrophoneEnabled = true;
            _appearance.SetData(uid, RadioDeviceVisuals.Broadcasting, true);
        }
    }
    // End Frontier
}
