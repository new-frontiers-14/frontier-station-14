using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private void OnGunVerbExamine(Entity<GunComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetGunExamine(ent);

        var ev = new GunExamineEvent(examineMarkup);
        RaiseLocalEvent(ent, ref ev);

        _examine.AddDetailedExamineVerb(args,
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

        // Recoil (AngleIncrease)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-recoil",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.AngleIncreaseModified.Degrees, 2))
        ));

        // Stability (AngleDecay)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-stability",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.AngleDecayModified.Degrees, 2))
        ));

        // Max Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-max-angle",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.MaxAngleModified.Degrees, 2))
        ));

        // Min Angle
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-min-angle",
            ("color", FireRateExamineColor),
            ("value", MathF.Round((float)ent.Comp.MinAngleModified.Degrees, 2))
        ));

        // Fire Rate (converted from RPS to RPM)
        var fireRate = 0f;
        if (ent.Comp.SelectedMode != SelectiveFire.Burst)
            fireRate = ent.Comp.FireRateModified;
        else
            fireRate = ent.Comp.BurstFireRate;

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-fire-rate",
            ("color", FireRateExamineColor),
            ("value", MathF.Round(fireRate, 1).ToString("0.0"))
        ));

        // Muzzle Velocity (ProjectileSpeed * 10)
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("gun-examine-muzzle-velocity",
            ("color", FireRateExamineColor),
            ("value", MathF.Round(ent.Comp.ProjectileSpeedModified, 0))
        ));

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
