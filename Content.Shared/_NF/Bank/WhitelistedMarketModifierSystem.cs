using Content.Shared.Examine;
using Content.Shared._NF.Bank.Components;
using Content.Shared.VendingMachines;

namespace Content.Shared._NF.Bank;

// Based on MarketModifierSystem
public sealed partial class WhitelistedMarketModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhitelistedMarketModifierComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<WhitelistedMarketModifierComponent> ent, ref ExaminedEvent args)
    {
        // If the machine is a vendor, don't print out rates
        if (HasComp<VendingMachineComponent>(ent))
            return;

        if (ent.Comp.Description != null)
        {
            var type = Loc.GetString(ent.Comp.Description);
            if (ent.Comp.Mod >= 1.0f)
                args.PushMarkup(Loc.GetString($"whitelisted-market-modifier-sell-high", ("mod", ent.Comp.Mod), ("type", type)));
            else
                args.PushMarkup(Loc.GetString($"whitelisted-market-modifier-sell-low", ("mod", ent.Comp.Mod), ("type", type)));
        }
        else
        {
            if (ent.Comp.Mod >= 1.0f)
                args.PushMarkup(Loc.GetString($"whitelisted-market-modifier-sell-high-unknown", ("mod", ent.Comp.Mod)));
            else
                args.PushMarkup(Loc.GetString($"whitelisted-market-modifier-sell-low-unknown", ("mod", ent.Comp.Mod)));
        }
    }
}
