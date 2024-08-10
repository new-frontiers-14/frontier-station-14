using Content.Shared.Examine;
using Content.Shared.Bank.Components;
using Content.Shared.VendingMachines;

namespace Content.Shared.Bank;

public sealed partial class MarketModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarketModifierComponent, ExaminedEvent>(OnExamined);
    }

    // This code is licensed under AGPLv3. See AGPLv3.txt
    private void OnExamined(Entity<MarketModifierComponent> ent, ref ExaminedEvent args)
    {
        // If the machine is a vendor, don't print out rates
        if (HasComp<VendingMachineComponent>(ent))
            return;

        string locVerb = ent.Comp.Buy ? "buy" : "sell";
        if (ent.Comp.Mod >= 1.0f)
            args.PushMarkup(Loc.GetString($"market-modifier-{locVerb}-high", ("mod", ent.Comp.Mod)));
        else
            args.PushMarkup(Loc.GetString($"market-modifier-{locVerb}-low", ("mod", ent.Comp.Mod)));
    }
}
