using System.Linq;
using Content.Server.Popups;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Language;
using static Content.Server.Language.LanguageSystem;

namespace Content.Server.Language;

// this does not support holding multiple translators at once yet.
// that should not be an issue for now, but it better get fixed later.
public sealed class TranslatorSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IntrinsicTranslatorComponent, DetermineEntityLanguagesEvent>(ApplyTranslation);

        SubscribeLocalEvent<HandheldTranslatorComponent, InteractEvent>(OnTranslatorToggle);
        SubscribeLocalEvent<HandheldTranslatorComponent, EquippedEventBase>(OnEquipTranslator);
        SubscribeLocalEvent<HandheldTranslatorComponent, EquippedHandEvent>(OnHandEquipTranslator);
        SubscribeLocalEvent<HandheldTranslatorComponent, UnequippedEventBase>(OnUnequipTranslator);
        SubscribeLocalEvent<HandheldTranslatorComponent, UnequippedHandEvent>(OnHandUnequipTranslator);
    }

    private void ApplyTranslation(EntityUid uid, IntrinsicTranslatorComponent component,
        DetermineEntityLanguagesEvent ev)
    {
        if (!component.Enabled)
            return;

        foreach (var language in component.SpokenLanguages)
        {
            AddIfNotExists(ev.SpokenLanguages, language);
        }

        foreach (var language in component.UnderstoodLanguages)
        {
            AddIfNotExists(ev.UnderstoodLanguages, language);
        }

        if (component.CurrentSpeechLanguage != null && ev.CurrentLanguage.Length == 0)
        {
            ev.CurrentLanguage = component.CurrentSpeechLanguage;
        }
    }

    private void OnEquipTranslator(EntityUid uid, HandheldTranslatorComponent component, EquippedEventBase args)
    {
        // TODO: make this customizable?
        if (args.SlotFlags.HasFlag(SlotFlags.POCKET) || args.SlotFlags.HasFlag(SlotFlags.NECK))
            return;
        TranslatorEquipped(args.Equipee, uid, component);
    }

    private void OnHandEquipTranslator(EntityUid uid, HandheldTranslatorComponent component, EquippedHandEvent args)
    {
        TranslatorEquipped(args.User, uid, component);
    }

    private void OnUnequipTranslator(EntityUid uid, HandheldTranslatorComponent component, UnequippedEventBase args)
    {
        TranslatorUnequipped(args.Equipee, uid, component);
    }

    private void OnHandUnequipTranslator(EntityUid uid, HandheldTranslatorComponent component, UnequippedHandEvent args)
    {
        TranslatorUnequipped(args.User, uid, component);
    }

    private void TranslatorEquipped(EntityUid holder, EntityUid translator, HandheldTranslatorComponent component)
    {
        if (!EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
            return;

        var intrinsic = EntityManager.EnsureComponent<HoldsTranslatorComponent>(holder);
        UpdateBoundIntrinsicComp(component, intrinsic, component.Enabled);
    }

    private void TranslatorUnequipped(EntityUid holder, EntityUid translator, HandheldTranslatorComponent component)
    {
        if (!EntityManager.TryGetComponent<HoldsTranslatorComponent>(holder, out var intrinsic))
            return;

        if (intrinsic.Issuer == component)
        {
            intrinsic.Enabled = false;
            EntityManager.RemoveComponent<HoldsTranslatorComponent>(holder);
        }
    }

    private void OnTranslatorToggle(EntityUid uid, HandheldTranslatorComponent component, InteractEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        if (Transform(uid).ParentUid is { Valid: true } holder && EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
        {
            // This translator is held by a language speaker and thus has an intrinsic counterpart bound to it. Make sure it's up-to-date.
            var intrinsic = EntityManager.EnsureComponent<HoldsTranslatorComponent>(holder);
            var isEnabled = component.Enabled;
            if (intrinsic.Issuer != component)
            {
                // The intrinsic comp wasn't owned by this handheld component, so this comp wasn't the active translator.
                // Thus it needs to be turned on regardless of its previous state.
                intrinsic.Issuer = component;
                isEnabled = true;
            }

            UpdateBoundIntrinsicComp(component, intrinsic, isEnabled);
            component.Enabled = isEnabled;
        }
        else
        {
            // This is a standalone translator (e.g. lying on the ground). Simply toggle its state.
            component.Enabled = !component.Enabled;
        }

        var message = Loc.GetString(component.Enabled ? "translator-component-turnon" : "translator-component-shutoff");
        _popup.PopupEntity(message, component.Owner, args.User);
    }

    /// <summary>
    ///   Copies the state from the handheld [comp] to the [intrinsic] comp, using [isEnabled] as the enabled state.
    /// </summary>
    private void UpdateBoundIntrinsicComp(HandheldTranslatorComponent comp, HoldsTranslatorComponent intrinsic, bool isEnabled)
    {
        if (isEnabled)
        {
            intrinsic.SpokenLanguages = new List<string>(comp.SpokenLanguages);
            intrinsic.UnderstoodLanguages = new List<string>(comp.UnderstoodLanguages);
            intrinsic.CurrentSpeechLanguage = comp.CurrentSpeechLanguage;
        }
        else
        {
            intrinsic.SpokenLanguages.Clear();
            intrinsic.UnderstoodLanguages.Clear();
            intrinsic.CurrentSpeechLanguage = null;
        }

        intrinsic.Enabled = isEnabled;
    }

    private static void AddIfNotExists(List<string> list, string item)
    {
        if (list.Contains(item))
            return;
        list.Add(item);
    }
}
