using Content.Shared.Clothing;
using Content.Shared._NF.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Robust.Shared.Utility;
using System.Linq;

public sealed class ClothingDamageModifierSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    private int _totalContextCount;
    public override void Initialize()
    {
        _totalContextCount = Enum.GetValues<DamageContext>().Length;
        SubscribeLocalEvent<ClothingDamageModifierComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClothingDamageModifierComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<WearerDamageModifierComponent, ApplyClothingDamageModifierEvent>(OnApplyDamageModifiers);
        SubscribeLocalEvent<WearerDamageModifierComponent, ApplyClothingStaminaModifierEvent>(OnApplyStaminaModifiers);
        SubscribeLocalEvent<ClothingDamageModifierComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);
    }

    private void OnEquipped(EntityUid uid, ClothingDamageModifierComponent comp, ref ClothingGotEquippedEvent args)
    {
        var wearer = EnsureComp<WearerDamageModifierComponent>(args.Wearer);
        wearer.Sources.Add(uid);
    }

    private void OnUnequipped(EntityUid uid, ClothingDamageModifierComponent comp, ref ClothingGotUnequippedEvent args)
    {
        if (TryComp<WearerDamageModifierComponent>(args.Wearer, out var wearer))
        {
            wearer.Sources.Remove(uid);
        }
    }

    private void OnApplyDamageModifiers(EntityUid uid, WearerDamageModifierComponent comp, ref ApplyClothingDamageModifierEvent args)
    {
        if (comp.Sources.Count == 0)
            return;

        foreach (var sourceUid in comp.Sources)
        {
            if (!TryComp<ClothingDamageModifierComponent>(sourceUid, out var source))
                continue;

            if (!source.AppliesTo(args.Context))
                continue;

            var flat = source.GetFlatBonus(args.Context);
            if (flat != null)
                args.Damage += flat;

            var mod = source.GetModifierSet(args.Context);
            if (mod != null)
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, mod);
        }

    }

    private void OnApplyStaminaModifiers(EntityUid uid, WearerDamageModifierComponent comp, ref ApplyClothingStaminaModifierEvent args)
    {
        if (comp.Sources.Count == 0)
            return;

        foreach (var sourceUid in comp.Sources)
        {
            if (!TryComp<ClothingDamageModifierComponent>(sourceUid, out var source))
                continue;

            if (!source.AppliesTo(args.Context))
                continue;

            args.StaminaDamage *= source.StaminaMultiplier ?? 1f;
        }

        foreach (var sourceUid in comp.Sources)
        {
            if (!TryComp<ClothingDamageModifierComponent>(sourceUid, out var source))
                continue;

            if (!source.AppliesTo(args.Context))
                continue;

            args.StaminaDamage += source.StaminaFlatBonus ?? 0f;
        }
    }

    private void OnClothingVerbExamine(EntityUid uid, ClothingDamageModifierComponent component, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.BonusDamage == null && component.DamageModifierSet == null)
            return;

        var examineMarkup = new FormattedMessage();
        GetCombatStyleDetails(examineMarkup, component);
        GetDamageDetails(examineMarkup, component.BonusDamage, component.DamageModifierSet);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup, Loc.GetString("clothing-damage-examinable-verb-text"), "/Textures/Interface/VerbIcons/knife.svg.192dpi.png", Loc.GetString("clothing-damage-examinable-verb-message"));
    }

    private void GetCombatStyleDetails(FormattedMessage msg, ClothingDamageModifierComponent component)
    {
        if (component.Affects.Count == 0)
        {
            msg.AddMarkupOrThrow(Loc.GetString("clothing-damage-no-modifiers-present"));
            return;
        }

        if (component.Affects.Count == _totalContextCount)
        {
            msg.AddMarkupOrThrow(Loc.GetString("clothing-damage-modifiers-all-contexts"));
            return;
        }

        var contextNames = component.Affects.Select(ctx => Loc.GetString($"damage-context-{ctx.ToString().ToLower()}")).ToArray();

        var contextsText = string.Join(", ", contextNames);

        msg.AddMarkupOrThrow(Loc.GetString("clothing-damage-modifiers-present-for-contexts", ("contexts", contextsText)));
    }

    private void GetDamageDetails(FormattedMessage msg, DamageSpecifier? bonus, DamageModifierSet? modifiers)
    {
        if (bonus == null && modifiers == null)
        {
            msg.AddMarkupOrThrow(Loc.GetString($"clothing-damage-no-changes"));
        }

        if (bonus != null)
        {
            foreach (var (type, amount) in bonus.DamageDict)
            {
                if (amount == 0)
                    continue;

                if (amount > 0)
                {
                    msg.PushNewline();
                    msg.AddMarkupOrThrow(Loc.GetString($"clothing-damage-flat-increase", ("type", type), ("amount", MathF.Round((float)amount, 1))));
                }
                else
                {
                    msg.PushNewline();
                    msg.AddMarkupOrThrow(Loc.GetString($"clothing-damage-flat-decrease", ("type", type), ("amount", MathF.Abs(MathF.Round((float)amount, 1)))));
                }
            }
        }

        if (modifiers != null)
        {
            if (bonus != null)
            {
                msg.PushNewline();
            }

            foreach (var (type, amount) in modifiers.Coefficients)
            {
                if (amount == 1)
                    continue;
                if (amount > 1)
                {
                    msg.PushNewline();
                    msg.AddMarkupOrThrow(Loc.GetString($"clothing-damage-coefficient-increase", ("type", type), ("amount", MathF.Round(((float)amount - 1f) * 100, 1))));
                }
                if (amount < 1 && amount != 0)
                {
                    msg.PushNewline();
                    msg.AddMarkupOrThrow(Loc.GetString($"clothing-damage-coefficient-decrease", ("type", type), ("amount", MathF.Round(((float)amount - 1f) * 100, 1))));
                }
            }
        }
    }
}