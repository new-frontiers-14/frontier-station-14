using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Content.Shared.Mobs.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Server.GameObjects; // Frontier
using Content.Server.Station.Systems; // Frontier
using Content.Shared.Humanoid; // Frontier

namespace Content.Server.Trigger.Systems;

public sealed class RattleOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly TransformSystem _transform = default!; // Frontier
    [Dependency] private readonly StationSystem _station = default!; // Frontier

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

        // Frontier: more specific species, grid and coordinate messages

        // // Gets the location of the user
        // var posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(target.Value));
        // var message = Loc.GetString(messageId, ("user", target.Value), ("position", posText));

        // Gets location of the implant
        var pos = _transform.GetMapCoordinates(target.Value);
        var x = (int)pos.X;
        var y = (int)pos.Y;
        var posText = $"({x}, {y})";

        // Frontier: Gets station location of the implant
        var station = _station.GetOwningStation(ent);
        var stationText = station is null ? "" : $"{Name(station.Value)} ";

        // Frontier: Gets species of the implant user
        var speciesText = $"";
        if (TryComp<HumanoidAppearanceComponent>(target.Value, out var species))
            speciesText = $" ({species!.Species})";

        var message = Loc.GetString(messageId, ("user", target.Value), ("specie", speciesText), ("grid", stationText!), ("position", posText));
        // End Frontier

        // Sends a message to the radio channel specified by the implant
        _radio.SendRadioMessage(ent.Owner, message, _prototypeManager.Index(ent.Comp.RadioChannel), ent.Owner);
    }
}