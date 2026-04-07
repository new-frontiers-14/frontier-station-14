using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Radio;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants;

public sealed class SwanSongImplantSystem : EntitySystem
{
    private const int MaxMessageLength = 128;
    private static readonly ProtoId<RadioChannelPrototype> CommonChannel = "Common";
    private static readonly ProtoId<RadioChannelPrototype> MedicalChannel = "Medical";

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SwanSongImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SwanSongImplantComponent, DistressImplantSetMessageMessage>(OnSetMessage);
        SubscribeLocalEvent<SwanSongImplantComponent, DistressImplantSetModeMessage>(OnSetMode);
        SubscribeLocalEvent<SwanSongImplantComponent, ImplantRelayEvent<MobStateChangedEvent>>(OnMobStateChanged);
        SubscribeLocalEvent<DistressImplantOpenMenuEvent>(OnOpenMenu);
    }

    private void OnImplanted(Entity<SwanSongImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is not { } implanted)
            return;

        _metaData.SetEntityName(ent.Owner, GetTrackerName(implanted));
    }

    private void OnOpenMenu(DistressImplantOpenMenuEvent args)
    {
        var implant = args.Action.Comp.Container;

        if (implant is not { } implantUid ||
            !TryComp<SwanSongImplantComponent>(implantUid, out var component))
            return;

        if (!_ui.HasUi(implantUid, DistressImplantUiKey.Key))
            return;

        _ui.OpenUi(implantUid, DistressImplantUiKey.Key, args.Performer);
        UpdateUi((implantUid, component));
    }

    private void OnSetMode(Entity<SwanSongImplantComponent> ent, ref DistressImplantSetModeMessage args)
    {
        ent.Comp.OutputMode = args.Mode;
        Dirty(ent);
        UpdateUi(ent);

        if (TryComp<SubdermalImplantComponent>(ent, out var subdermal) &&
            subdermal.ImplantedEntity is { } implanted)
        {
            _popup.PopupEntity($"Distress implant channel set to {GetModeName(ent.Comp.OutputMode)}.", implanted, implanted);
        }
    }

    private void OnSetMessage(Entity<SwanSongImplantComponent> ent, ref DistressImplantSetMessageMessage args)
    {
        var message = args.Message.Trim();
        if (message.Length == 0 || message.Length > MaxMessageLength)
            return;

        ent.Comp.Message = message;
        Dirty(ent);
        UpdateUi(ent);

        if (TryComp<SubdermalImplantComponent>(ent, out var subdermal) &&
            subdermal.ImplantedEntity is { } implanted)
        {
            _popup.PopupEntity("Distress implant message updated.", implanted, implanted);
        }
    }

    private void OnMobStateChanged(Entity<SwanSongImplantComponent> ent, ref ImplantRelayEvent<MobStateChangedEvent> args)
    {
        if (args.Event.OldMobState >= MobState.Critical || args.Event.NewMobState != MobState.Critical)
            return;

        if (!TryComp<SubdermalImplantComponent>(ent, out var subdermal) ||
            subdermal.ImplantedEntity is not { } implanted)
            return;

        if (ent.Comp.LastTriggerTime is { } lastTrigger &&
            _timing.CurTime < lastTrigger + ent.Comp.TriggerCooldown)
            return;

        ent.Comp.LastTriggerTime = _timing.CurTime;

        var trackerName = GetTrackerName(implanted);

        switch (ent.Comp.OutputMode)
        {
            case SwanSongOutputMode.Local:
                _chat.TrySendInGameICMessage(
                    implanted,
                    ent.Comp.Message,
                    InGameICChatType.Speak,
                    false,
                    nameOverride: trackerName,
                    checkRadioPrefix: false,
                    ignoreActionBlocker: true);
                break;

            case SwanSongOutputMode.Common:
                _radio.SendRadioMessage(ent.Owner, ent.Comp.Message, _prototype.Index(CommonChannel), implanted, escapeMarkup: false);
                break;

            case SwanSongOutputMode.Medical:
                _radio.SendRadioMessage(ent.Owner, ent.Comp.Message, _prototype.Index(MedicalChannel), implanted, escapeMarkup: false);
                break;
        }
    }

    private void UpdateUi(Entity<SwanSongImplantComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, DistressImplantUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, DistressImplantUiKey.Key, new DistressImplantBuiState(ent.Comp.Message, ent.Comp.OutputMode));
    }

    private static string GetModeName(SwanSongOutputMode mode)
    {
        return mode switch
        {
            SwanSongOutputMode.Local => "Local",
            SwanSongOutputMode.Common => "Common",
            SwanSongOutputMode.Medical => "Medical",
            _ => "Unknown"
        };
    }

    private string GetTrackerName(EntityUid implanted)
    {
        return $"{MetaData(implanted).EntityName}'s tracker";
    }
}
