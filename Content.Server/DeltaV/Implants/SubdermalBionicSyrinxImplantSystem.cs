using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.VoiceMask;
using Content.Server.Implants;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Tag;
using Content.Shared.VoiceMask;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.DeltaV.Implants;

public sealed class SubdermalBionicSyrinxImplantSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string BionicSyrinxImplant = "BionicSyrinxImplant";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceMaskerComponent, ImplantImplantedEvent>(OnInsert);
        SubscribeLocalEvent<SyrinxVoiceMaskComponent, TransformSpeakerNameEvent>(OnSpeakerNameTransform);
        SubscribeLocalEvent<SyrinxVoiceMaskComponent, VoiceMaskChangeNameMessage>(OnChangeName);
        // We need to remove the SyrinxVoiceMaskComponent from the owner before the implant
        // is removed, so we need to execute before the SubdermalImplantSystem.
        SubscribeLocalEvent<VoiceMaskerComponent, EntGotRemovedFromContainerMessage>(OnRemove, before: new[] { typeof(SubdermalImplantSystem) });
    }

    private void OnInsert(EntityUid uid, VoiceMaskerComponent component, ImplantImplantedEvent args)
    {
        if (!args.Implanted.HasValue ||
            !_tag.HasTag(args.Implant, BionicSyrinxImplant))
            return;

        var voicemask = EnsureComp<SyrinxVoiceMaskComponent>(args.Implanted.Value);
        voicemask.VoiceName = MetaData(args.Implanted.Value).EntityName;
        Dirty(args.Implanted.Value, voicemask);
    }

    private void OnRemove(EntityUid uid, VoiceMaskerComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<SubdermalImplantComponent>(uid, out var implanted) || implanted.ImplantedEntity == null)
            return;

        RemComp<SyrinxVoiceMaskComponent>(implanted.ImplantedEntity.Value);
    }

    /// <summary>
    /// Copy from VoiceMaskSystem, adapted to work with SyrinxVoiceMaskComponent.
    /// </summary>
    private void OnChangeName(EntityUid uid, SyrinxVoiceMaskComponent component, VoiceMaskChangeNameMessage message)
    {
        if (message.Name.Length > HumanoidCharacterProfile.MaxNameLength || message.Name.Length <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-failure"), uid, message.Actor, PopupType.SmallCaution);
            return;
        }

        component.VoiceName = message.Name;
        if (message.Actor != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(message.Actor):player} set voice of {ToPrettyString(uid):mask}: {component.VoiceName}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"Voice of {ToPrettyString(uid):mask} set: {component.VoiceName}");

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-popup-success"), uid, message.Actor);
        TrySetLastKnownName(uid, message.Name);
        UpdateUI(uid, component);
    }

    /// <summary>
    /// Copy from VoiceMaskSystem, adapted to work with SyrinxVoiceMaskComponent.
    /// </summary>
    private void TrySetLastKnownName(EntityUid implanted, string lastName)
    {
        if (!HasComp<VoiceMaskComponent>(implanted)
            || !TryComp<VoiceMaskerComponent>(implanted, out var maskComp))
            return;

        maskComp.LastSetName = lastName;
    }

    /// <summary>
    /// Copy from VoiceMaskSystem, adapted to work with SyrinxVoiceMaskComponent.
    /// </summary>
    private void UpdateUI(EntityUid owner, SyrinxVoiceMaskComponent? component = null)
    {
        if (!Resolve(owner, ref component, logMissing: false))
            return;

        _uiSystem.SetUiState(owner, VoiceMaskUIKey.Key, new VoiceMaskBuiState(component.VoiceName, null));
    }

    /// <summary>
    /// Copy from VoiceMaskSystem, adapted to work with SyrinxVoiceMaskComponent.
    /// </summary>
    private void OnSpeakerNameTransform(EntityUid uid, SyrinxVoiceMaskComponent component, TransformSpeakerNameEvent args)
    {
        if (component.Enabled)
            args.Name = component.VoiceName;
    }
}
