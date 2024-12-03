using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Shared._NF.Trade.Components;
using Content.Shared.Cargo.Components;

namespace Content.Server.Corvax.Inflation;

public sealed class InflationCargoCrateSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private TimeSpan? Timer = TimeSpan.FromMinutes(5);
    private TimeSpan? NextTimeToCheck = TimeSpan.FromSeconds(5);

    StaticPriceComponent? staticPriceComponent = null;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        int numberCrates = 0;

        double modifier = 0;

        if (NextTimeToCheck < _gameTiming.CurTime)
        {
            numberCrates = _entManager.Count<TradeCrateComponent>();

            var query = EntityQueryEnumerator<InflationCargoCrateComponent>();
            while (query.MoveNext(out var uid, out var inflationCargoCrateComponent))
            {
                var xformQuery = GetEntityQuery<StaticPriceComponent>();
                if (!xformQuery.TryGetComponent(uid, out var xform))
                {
                    return;
                }

                if (numberCrates >= 1 && numberCrates <= 19)
                    modifier = 1;
                else if (numberCrates >= 20 && numberCrates <= 29)
                    modifier = 0.8;
                else if (numberCrates >= 30 && numberCrates <= 49)
                    modifier = 0.75;
                else if (numberCrates >= 50)
                    modifier = 0.68;

                foreach (var iterator in _entManager.EntityQuery<TradeCrateComponent>(true))
                {

                    if (TryComp(uid, out inflationCargoCrateComponent))
                    {
                        if (inflationCargoCrateComponent.IsInflated)
                            continue;

                        if (TryComp(uid, out staticPriceComponent))
                        {
                            staticPriceComponent.Price *= modifier;
                            inflationCargoCrateComponent.IsInflated = true;
                        }

                        if (iterator.Owner == uid)
                        continue;
                    }
                }
            }
            NextTimeToCheck = NextTimeToCheck + Timer;
        }
    }
}
