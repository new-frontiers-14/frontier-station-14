using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Shared.Mobs.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Trigger.Systems;

public sealed class RattleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RattleOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RattleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<MobStateComponent>(target.Value, out var mobstate))
            return;

        args.Handled = true;

        if (!ent.Comp.Messages.TryGetValue(mobstate.CurrentState, out var messageId))
            return;

        // Gets the location of the user
        var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(target.Value));

        var message = Loc.GetString(messageId, ("user", target.Value), ("position", posText));
        // Sends a message to the radio channel specified by the implant
        _radio.SendRadioMessage(ent.Owner, message, _prototypeManager.Index(ent.Comp.RadioChannel), ent.Owner);
    }
}

/*
Frontier upstream merge TODO: shove this V code into that code ^

        // Frontier: custom function implementation
        private void HandleRattleTrigger(EntityUid uid, RattleComponent component, TriggerEvent args)
        {
            if (!TryComp<SubdermalImplantComponent>(uid, out var implanted))
                return;

            if (implanted.ImplantedEntity == null)
                return;

            // Gets location of the implant
            var ownerXform = Transform(uid);
            var pos = ownerXform.MapPosition;
            var x = (int) pos.X;
            var y = (int) pos.Y;
            var posText = $"({x}, {y})";

            // Frontier: Gets station location of the implant
            var station = _station.GetOwningStation(uid);
            var stationText = station is null ? null : $"{Name(station.Value)} ";

            if (stationText == null)
                stationText = "";

            // Frontier: Gets species of the implant user
            var speciesText = $"";
            if (TryComp<HumanoidAppearanceComponent>(implanted.ImplantedEntity, out var species))
                speciesText = $" ({species!.Species})";

            var critMessage = Loc.GetString(component.CritMessage, ("user", implanted.ImplantedEntity.Value), ("specie", speciesText), ("grid", stationText!), ("position", posText));
            var deathMessage = Loc.GetString(component.DeathMessage, ("user", implanted.ImplantedEntity.Value), ("specie", speciesText), ("grid", stationText!), ("position", posText));

            if (!TryComp<MobStateComponent>(implanted.ImplantedEntity, out var mobstate))
                return;

            if (mobstate.CurrentState != MobState.Alive)
            {
                // Sends a message to the radio channel specified by the implant
                if (mobstate.CurrentState == MobState.Critical)
                    _radioSystem.SendRadioMessage(uid, critMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);
                if (mobstate.CurrentState == MobState.Dead)
                    _radioSystem.SendRadioMessage(uid, deathMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);
            }

            args.Handled = true;
        }
        // End Frontier
        */