using System.Diagnostics.CodeAnalysis;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    private void OnGunVerbExamine(Entity<GunComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetGunExamine(ent);

        var ev = new GunExamineEvent(examineMarkup);
        RaiseLocalEvent(ent, ref ev);

        Examine.AddDetailedExamineVerb(args, // Frontier: use SharedGunSystem's examine member
            ent.Comp,
            examineMarkup,
            Loc.GetString("gun-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("gun-examinable-verb-message"));
    }

    private FormattedMessage GetGunExamine(Entity<GunComponent> ent)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine"));

        // Frontier: use nf-prefixed loc strings, no rounding on values
        // Recoil (AngleIncrease)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-recoil",
            ("color", FireRateExamineColor),
            ("value", ent.Comp.AngleIncreaseModified.Degrees)
        ));

        // Stability (AngleDecay)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-stability",
            ("color", FireRateExamineColor),
            ("value", ent.Comp.AngleDecayModified.Degrees)
        ));

        // Max Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-max-angle",
            ("color", FireRateExamineColor),
            ("value", ent.Comp.MaxAngleModified.Degrees)
        ));

        // Min Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-min-angle",
            ("color", FireRateExamineColor),
            ("value", ent.Comp.MinAngleModified.Degrees)
        ));

        // Frontier: separate burst fire calculation
        // Fire Rate (converted from RPS to RPM)
        if (ent.Comp.SelectedMode != SelectiveFire.Burst)
        {
            var fireRate = ent.Comp.FireRateModified;
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-fire-rate",
                ("color", FireRateExamineColor),
                ("value", fireRate)
            ));
        }
        else
        {
            var fireRate = ent.Comp.ShotsPerBurstModified / (ent.Comp.BurstCooldown + (ent.Comp.ShotsPerBurstModified - 1) / ent.Comp.BurstFireRate);
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-fire-rate-burst",
                ("color", FireRateExamineColor),
                ("value", fireRate),
                ("burstsize", ent.Comp.ShotsPerBurstModified),
                ("burstrate", ent.Comp.BurstFireRate)
            ));
        }
        // End Frontier: separate burst fire calculation

        // Muzzle Velocity (ProjectileSpeed)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-nf-muzzle-velocity",
            ("color", FireRateExamineColor),
            ("value", ent.Comp.ProjectileSpeedModified)
        ));
        // End Frontier: use nf-prefixed loc strings, no rounding on values

        return msg;
    }

    private bool TryGetGunCaliber(EntityUid uid, GunComponent component, [NotNullWhen(true)] out string? caliber)
    {
        caliber = null;

        // Frontier change: Added ExamineCaliber to guns to note the caliber type in ftl
        if (!string.IsNullOrEmpty(component.ExamineCaliber))
        {
            var caliberName = Loc.GetString(component.ExamineCaliber);

            caliber = caliberName;
            return true;
        }

        return false;
    }

    private void InitializeGunExamine()
    {
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<ExamineVerb>>(OnGunVerbExamine);
    }
}

/// <summary>
/// Event raised on a gun entity to get additional examine text relating to its specifications.
/// </summary>
[ByRefEvent]
public readonly record struct GunExamineEvent(FormattedMessage Msg);
