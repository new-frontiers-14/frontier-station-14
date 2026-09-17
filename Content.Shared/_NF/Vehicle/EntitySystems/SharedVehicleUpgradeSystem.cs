using Content.Shared._NF.Vehicle.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Vehicle.EntitySystems;

public abstract class SharedVehicleUpgradeSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private const string UpgradeIconPath = "/Textures/Interface/VerbIcons/pickup.svg.192dpi.png";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleUpgradeComponent, GetVerbsEvent<ExamineVerb>>(OnVehicleVerbExamine);
    }

    private void OnVehicleVerbExamine(Entity<VehicleUpgradeComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        GetSpeedModifierExamine(markup, ent);

        _examine.AddDetailedExamineVerb(args, ent.Comp, markup,
            Loc.GetString("vehicle-verb-examinable-upgrades-text"),
            UpgradeIconPath,
            Loc.GetString("vehicle-verb-examinable-upgrades-message"));
    }

    private void GetSpeedModifierExamine(FormattedMessage msg, Entity<VehicleUpgradeComponent> ent)
    {
        var percent = Math.Round(100 * MathF.Abs(ent.Comp.CurrentSpeedModifier - 1), 2);
        var locId = ent.Comp.CurrentSpeedModifier switch
        {
            < 1 => "vehicle-upgrade-speed-decreased",
            1 or float.NaN => "vehicle-upgrade-speed-not-upgraded",
            > 1 => "vehicle-upgrade-speed-increased",
        };
        msg.AddMarkupOrThrow(Loc.GetString(locId, ("percent", percent)));
    }
}
