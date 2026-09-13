using System.Text.RegularExpressions;
using Content.Server._NF.Speech.Components;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Speech;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class SiliconAccentSystem : EntitySystem
{
    private static readonly Regex ABeforeVowelSound = new(
        @"\b([Aa]) (?=(?!(?:unit|user|university|united|unique|one|euro|ewe)\b)[aeiou])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AnBeforeConsonantSound = new(
        @"\b([Aa])n (?=(?!(?:hour|honest|honor|h2o)\b)[b-df-hj-np-tv-z])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex MidSentenceThisUnit = new(
        @"(?<=[\p{Ll}\d,;:] )This unit\b",
        RegexOptions.CultureInvariant);

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SiliconAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SiliconAccentComponent component, AccentGetEvent args)
    {
        if (HasComp<BorgChassisComponent>(uid) && HasEquipmentAccent(uid))
            return;

        args.Message = CorrectGrammar(_replacement.ApplyReplacements(args.Message, "silicon_accent"));
    }

    internal static string CorrectGrammar(string message)
    {
        message = MidSentenceThisUnit.Replace(message, "this unit");
        return CorrectArticles(message);
    }

    internal static string CorrectArticles(string message)
    {
        message = ABeforeVowelSound.Replace(message,
            match => match.Groups[1].Value == "A" ? "An " : "an ");
        return AnBeforeConsonantSound.Replace(message,
            match => match.Groups[1].Value == "A" ? "A " : "a ");
    }

    private bool HasEquipmentAccent(EntityUid uid)
    {
        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.NextItem(out var item))
        {
            if (TryComp<AddAccentClothingComponent>(item, out var clothing) &&
                clothing.IsActive && clothing.Wearer == uid)
                return true;
        }

        foreach (var item in _hands.EnumerateHeld(uid))
        {
            if (TryComp<AddAccentPickupComponent>(item, out var pickup) &&
                pickup.IsActive && pickup.Holder == uid)
                return true;
        }

        return false;
    }
}
