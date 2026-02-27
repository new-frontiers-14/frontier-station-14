using Content.Server._NF.Radio.Components;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._NF.Radio;
using Content.Shared.Chat;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Radio.Systems;

public sealed partial class HandheldRadioSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldRadioComponent, ComponentInit>(OnRadioAdded);
        SubscribeLocalEvent<HandheldRadioComponent, ComponentShutdown>(OnRadioRemoved);

        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
        SubscribeLocalEvent<HandheldRadioComponent, InventoryRelayedEvent<SpeakHandheldRadioEvent>>(OnPlayerSpeakIntoRadio);
        SubscribeLocalEvent<HandheldRadioComponent, RadioReceiveEvent>(OnReceiveRadio);

        SubscribeLocalEvent<HandheldRadioComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<HandheldRadioComponent, ListenAttemptEvent>(OnAttemptListen);

        InitializeInteract();
    }

    private void OnRadioAdded(EntityUid uid, HandheldRadioComponent component, ComponentInit args)
    {
        // Set initial frequency to the channel frequency, if it's null
        //
        // This is done so that if the radio channel frequency is changed,
        // we don't have to go around and change the frequency in prototypes
        if (component.Frequency == null && _protoMan.TryIndex<RadioChannelPrototype>(component.Channel, out var channel))
        {
            component.Frequency = channel.Frequency;
        }

        EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;

        var radioComp = EnsureComp<ActiveRadioComponent>(uid);
        radioComp.Channels = new HashSet<String> { component.Channel };
    }

    private void OnRadioRemoved(EntityUid uid, HandheldRadioComponent component, ComponentShutdown args)
    {
        RemCompDeferred<ActiveRadioComponent>(uid);
        RemCompDeferred<ActiveListenerComponent>(uid);
    }

    /// <summary>
    /// Called anytime an entity speaks. This function relays the speaking event to hands + inventory slots,
    /// so that the handheld radio can pick it up.
    /// </summary>
    private void OnSpeak(EntitySpokeEvent ev)
    {
        var evt = new SpeakHandheldRadioEvent
        {
            SpeakEvent = ev
        };

        // Relay the speaking event to every inventory slot + hands.
        // So that any radio microphones in private mode/intercom mode can hear the player

        if (TryComp<InventoryComponent>(ev.Source, out var inventoryComp))
        {
            _inventory.RelayEvent((ev.Source, inventoryComp), ref evt);
        }

        if (TryComp<HandsComponent>(ev.Source, out var handsComp))
        {
            var invEvt = new InventoryRelayedEvent<SpeakHandheldRadioEvent>(evt, ev.Source);

            foreach (var held in _hands.EnumerateHeld((ev.Source, handsComp)))
            {
                var radioComponent = CompOrNull<HandheldRadioComponent>(held);
                if (radioComponent != null)
                {
                    RaiseLocalEvent(held, invEvt);
                }
            }
        }
    }

    /// <summary>
    /// Called by "OnSpeak" when:
    /// - The entity speaks into the handheld radio channel.
    /// - The entity is holding the handheld radio in its inventory or hands
    /// </summary>
    private void OnPlayerSpeakIntoRadio(Entity<HandheldRadioComponent> entity, ref InventoryRelayedEvent<SpeakHandheldRadioEvent> args)
    {
        if (entity.Comp.MicrophoneMode == HandheldRadioMode.Off)
            return;

        var channel = _protoMan.Index<RadioChannelPrototype>(entity.Comp.Channel)!;

        // Don't send the radio message if they're not speaking into the channel
        // that the handheld radio is currently set to
        if (channel != args.Args.SpeakEvent.Channel)
        {
            return;
        }

        //if (_recentlySent.Add((args.Message, args.Source, channel)))
        _radio.SendRadioMessage(args.Args.SpeakEvent.Source, args.Args.SpeakEvent.Message, channel, entity, frequency: entity.Comp.Frequency);
    }

    /// <summary>
    /// Called when anyone speaks on the radio
    /// </summary>
    private void OnReceiveRadio(EntityUid uid, HandheldRadioComponent component, ref RadioReceiveEvent args)
    {
        if (component.SpeakerMode == HandheldRadioMode.Off)
            return;

        var channel = _protoMan.Index<RadioChannelPrototype>(component.Channel)!;
        if (channel != args.Channel)
            return;

        // _radio.SendRadioMessage should filter this but it doesn't actually use the frequency param
        // to filter out receivers, so we have to do it
        var speakerRadioComponent = CompOrNull<HandheldRadioComponent>(args.RadioSource);
        if (speakerRadioComponent != null && speakerRadioComponent.Frequency != component.Frequency)
        {
            return;
        }

        switch (component.SpeakerMode)
        {
            case HandheldRadioMode.Private:
                var parent = Transform(uid).ParentUid;

                if (parent.IsValid())
                {
                    var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
                    RaiseLocalEvent(parent, ref relayEvent);
                }

                if (TryComp(parent, out ActorComponent? actor))
                    _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
                break;

            case HandheldRadioMode.Intercom:
                if (uid == args.RadioSource)
                    return;

                var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
                RaiseLocalEvent(args.MessageSource, nameEv);

                var name = Loc.GetString("speech-name-relay",
                    ("speaker", Name(uid)),
                    ("originalName", nameEv.VoiceName));

                _chat.TrySendInGameICMessage(uid, args.Message, component.OutputChatType, ChatTransmitRange.GhostRangeLimitNoAdminCheck, nameOverride: name, checkRadioPrefix: false);
                break;
        }
    }

    /// <summary>
    /// Called when anyone is speaking around the handheld radio within ListenRange.
    /// 
    /// This may not be called if the radio isn't in intercom mode or if the handheld
    /// radio requires an unobstructed path to whomever is speaking
    /// </summary>
    private void OnListen(EntityUid uid, HandheldRadioComponent component, ListenEvent args)
    {
        if (HasComp<HandheldRadioComponent>(args.Source))
            return; // no feedback loops please.

        var channel = _protoMan.Index<RadioChannelPrototype>(component.Channel)!;
        //if (_recentlySent.Add((args.Message, args.Source, channel)))
        _radio.SendRadioMessage(args.Source, args.Message, channel, uid, frequency: component.Frequency);
    }

    private void OnAttemptListen(EntityUid uid, HandheldRadioComponent component, ListenAttemptEvent args)
    {
        if (component.MicrophoneMode != HandheldRadioMode.Intercom)
        {
            args.Cancel();
            return;
        }

        if (component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0))
        {
            args.Cancel();
        }
    }
}
