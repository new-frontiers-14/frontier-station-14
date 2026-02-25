using Content.Server._NF.Radio.Components;
using Content.Server.Administration.Components;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared._NF.Radio.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.ComponentModel;

namespace Content.Server._NF.Radio.Systems;

public sealed class HandheldRadioSystem : SharedHandheldRadioSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldRadioComponent, ComponentInit>(OnRadioAdded);
        SubscribeLocalEvent<HandheldRadioComponent, ComponentShutdown>(OnRadioRemoved);

        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
        SubscribeLocalEvent<HandheldRadioComponent, InventoryRelayedEvent<SpeakHandheldRadioEvent>>(OnRadioReceiveSpeak);
        SubscribeLocalEvent<HandheldRadioComponent, RadioReceiveEvent>(OnReceiveRadio);
    }

    private void OnRadioAdded(EntityUid uid, HandheldRadioComponent component, ComponentInit args)
    {
        //var channel = _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel)!;
        var radioComp = EnsureComp<ActiveRadioComponent>(uid);
        radioComp.Channels.Add(component.Channel);
        //EnsureComp<ActiveRadioComponent>(uid).Channels.Add(component.Channel);
    }

    private void OnRadioRemoved(EntityUid uid, HandheldRadioComponent component, ComponentShutdown args)
    {
        RemCompDeferred<ActiveRadioComponent>(uid);
    }

    protected void OnSpeak(EntitySpokeEvent ev)
    {
        Log.Debug("Entity {entity} spoke!", ev.Source.ToString());

        var evt = new SpeakHandheldRadioEvent
        {
            SpeakEvent = ev
        };

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

    protected void OnRadioReceiveSpeak(Entity<HandheldRadioComponent> entity, ref InventoryRelayedEvent<SpeakHandheldRadioEvent> args)
    {
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

    protected void OnReceiveRadio(EntityUid uid, HandheldRadioComponent component, ref RadioReceiveEvent args)
    {
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

        //if (component.Frequency != args.RadioSource)

        //var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        //RaiseLocalEvent(args.MessageSource, nameEv);

        //var name = Loc.GetString("speech-name-relay",
        //    ("speaker", Name(uid)),
        //    ("originalName", nameEv.VoiceName));

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        //_chat.TrySendInGameICMessage(uid, args.Message, component.OutputChatType, ChatTransmitRange.GhostRangeLimitNoAdminCheck, nameOverride: name, checkRadioPrefix: false); // Frontier: GhostRangeLimit<GhostRangeLimitNoAdminCheck, InGameICChatType.Whisper<component.OutputChatType

        // TODO: change this when a code refactor is done
        // this is currently done this way because receiving radio messages on an entity otherwise requires that entity
        // to have an ActiveRadioComponent

        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
    }
}
