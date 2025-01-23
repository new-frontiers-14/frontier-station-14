using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;

namespace Content.Server.StationEvents.Events;

public sealed class IonStormRule : StationEventSystem<IonStormRuleComponent>
{
    [Dependency] private readonly IonStormSystem _ionStorm = default!;

    protected override void Started(EntityUid uid, IonStormRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // Frontier - Affect all silicon beings in the sector, not just on-station.
        // if (!TryGetRandomStation(out var chosenStation))
        //     return;
        // End Frontier

        var query = EntityQueryEnumerator<SiliconLawBoundComponent, TransformComponent, IonStormTargetComponent>();
        while (query.MoveNext(out var ent, out var lawBound, out var xform, out var target))
        {
            // Frontier - Affect all silicon beings in the sector, not just on-station.
            // // only affect law holders on the station
            // if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
            //     continue;
            // End Frontier

            _ionStorm.IonStormTarget((ent, lawBound, target));
        }
    }
}
