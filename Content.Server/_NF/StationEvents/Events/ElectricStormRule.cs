using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Random;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.Construction.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Station.Components;
using Robust.Server.Player;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server._NF.Power.EntitySystems;
using Content.Server._NF.Power.Components;

namespace Content.Server.StationEvents.Events;

public sealed class ElectricStormRule : StationEventSystem<ElectricStormRuleComponent>
{
    [Dependency] private readonly ElectricalOverloadSystem _electricalOverload = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const int MaxRetries = 10;
    private float _effectTimer = 0;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, ElectricStormRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        RaiseLocalEvent(uid, new ElectricalOverloadEvent(true));
    }

    protected override void Ended(EntityUid uid, ElectricStormRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        //RemComp<ElectricalOverloadComponent>(entity); // Umbra - ElectricalOverload
    }

    protected override void ActiveTick(EntityUid uid, ElectricStormRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        _effectTimer -= frameTime;
        if (_effectTimer < 0)
        {
            _effectTimer += 1;
            var computerQuery = EntityQueryEnumerator<ComputerComponent>();
            while (computerQuery.MoveNext(out var computerEnt, out var computer))
            {
                if (RobustRandom.Prob(component.ComputerChance))
                {
                    EnsureComp<ElectricalOverloadComponent>(computerEnt);
                }
            }
            var lightQuery = EntityQueryEnumerator<MachineComponent>();
            while (lightQuery.MoveNext(out var machineEnt, out var machine))
            {
                if (RobustRandom.Prob(component.MachineChance))
                {
                    EnsureComp<ElectricalOverloadComponent>(machineEnt);
                }
            }
        }
    }

    public record struct ElectricalOverloadEvent(bool Enabled);
}
