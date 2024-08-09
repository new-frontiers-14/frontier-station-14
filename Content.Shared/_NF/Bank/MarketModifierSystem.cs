using Content.Shared.Examine;
using Content.Shared.Bank.Components;
using Robust.Shared.Network;

namespace Content.Shared.Bank;

public abstract class MarketModifierSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarketModifierComponent, ExaminedEvent>(OnExamined);
    }

    // This code is licensed under AGPLv3. See AGPLv3.txt
    private void OnExamined(EntityUid uid, MarketModifierComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient)
            args.PushMarkup(Loc.GetString("market-modifier-green", ("mod", component.Mod)));
    }
}
