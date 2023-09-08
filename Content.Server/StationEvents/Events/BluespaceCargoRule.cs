using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
//using Content.Shared.Physics;
//using Robust.Shared.Map.Components;
//using Robust.Shared.Physics.Components;
//using Robust.Shared.Physics;
//using Robust.Shared.Configuration;
//using Content.Server.Station.Components;
//using Content.Server.Atmos.EntitySystems;
//using Robust.Shared.Map;
//using Content.Shared.Maps;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceCargoRule : StationEventSystem<BluespaceCargoRuleComponent>
{
    //[Dependency] private readonly IConfigurationManager _configuration = default!;
    //[Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    //[Dependency] protected readonly IRobustRandom Random = default!;
    //[Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    //[Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Added(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString("bluespace-cargo-event-announcement");
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    protected override void Started(EntityUid uid, BluespaceCargoRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var amountToSpawn = Math.Max(1, (int) MathF.Round(GetSeverityModifier() / 1.5f));
        for (var i = 0; i < amountToSpawn; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                return;

            //Spawn(component.CargoSpawnerPrototype, coords);
            //Spawn(component.CargoFlashPrototype, coords);

            //Sawmill.Info($"Spawning random cargo at {coords}");
        }
    }
}
