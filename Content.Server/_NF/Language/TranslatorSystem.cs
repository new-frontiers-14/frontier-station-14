using System.Linq;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Content.Shared.Language.Systems;
using Content.Shared.PowerCell;
using static Content.Server.Language.LanguageSystem;
using HandheldTranslatorComponent = Content.Shared.Language.Components.HandheldTranslatorComponent;
using HoldsTranslatorComponent = Content.Shared.Language.Components.HoldsTranslatorComponent;
using IntrinsicTranslatorComponent = Content.Shared.Language.Components.IntrinsicTranslatorComponent;

namespace Content.Server.Language;

// this does not support holding multiple translators at once yet.
// that should not be an issue for now, but it better get fixed later.
public sealed class TranslatorSystem : SharedTranslatorSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("translator");

        // I wanna die. But my death won't help us discover polymorphism.
        SubscribeLocalEvent<IntrinsicTranslatorComponent, DetermineEntityLanguagesEvent>(ApplyTranslation);
        SubscribeLocalEvent<HoldsTranslatorComponent, DetermineEntityLanguagesEvent>(ApplyTranslation);
        SubscribeLocalEvent<ImplantedTranslatorComponent, DetermineEntityLanguagesEvent>(ApplyTranslation);
        // TODO: make this thing draw power
        // SubscribeLocalEvent<HoldsTranslatorComponent, ListenEvent>(...);

        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorToggle);
        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);

        SubscribeLocalEvent<HandheldTranslatorComponent, InteractHandEvent>(
            (uid, component, args) => TranslatorEquipped(args.User, uid, component));
        SubscribeLocalEvent<HandheldTranslatorComponent, DroppedEvent>(
            (uid, component, args) => TranslatorUnequipped(args.User, uid, component));
    }

    private void ApplyTranslation(EntityUid uid, IntrinsicTranslatorComponent component,
        DetermineEntityLanguagesEvent ev)
    {
        if (!component.Enabled)
            return;

        if (!_powerCell.HasActivatableCharge(uid))
            return;

        var addUnderstood = true;
        var addSpoken = true;
        if (component.RequiredLanguages.Count > 0)
        {
            if (component.RequiresAllLanguages)
            {
                // Add langs when the wielder has all of the required languages
                foreach (var language in component.RequiredLanguages)
                {
                    if (!ev.SpokenLanguages.Contains(language, StringComparer.Ordinal))
                        addSpoken = false;

                    if (!ev.UnderstoodLanguages.Contains(language, StringComparer.Ordinal))
                        addUnderstood = false;
                }
            }
            else
            {
                // Add langs when the wielder has at least one of the required languages
                addUnderstood = false;
                addSpoken = false;
                foreach (var language in component.RequiredLanguages)
                {
                    if (ev.SpokenLanguages.Contains(language, StringComparer.Ordinal))
                        addSpoken = true;

                    if (ev.UnderstoodLanguages.Contains(language, StringComparer.Ordinal))
                        addUnderstood = true;
                }
            }
        }

        if (addSpoken)
        {
            foreach (var language in component.SpokenLanguages)
            {
                AddIfNotExists(ev.SpokenLanguages, language);
            }

            if (component.CurrentSpeechLanguage != null && ev.CurrentLanguage.Length == 0)
            {
                ev.CurrentLanguage = component.CurrentSpeechLanguage;
            }
        }

        if (addUnderstood)
        {
            foreach (var language in component.UnderstoodLanguages)
            {
                AddIfNotExists(ev.UnderstoodLanguages, language);
            }
        }
    }

    private void TranslatorEquipped(EntityUid holder, EntityUid translator, HandheldTranslatorComponent component)
    {
        if (!EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
            return;

        var intrinsic = EntityManager.EnsureComponent<HoldsTranslatorComponent>(holder);
        UpdateBoundIntrinsicComp(component, intrinsic, component.Enabled);

        UpdatedLanguages(holder);
    }

    private void TranslatorUnequipped(EntityUid holder, EntityUid translator, HandheldTranslatorComponent component)
    {
        if (!EntityManager.TryGetComponent<HoldsTranslatorComponent>(holder, out var intrinsic))
            return;

        if (intrinsic.Issuer == component)
        {

            intrinsic.Enabled = false;
            EntityManager.RemoveComponent(holder, intrinsic);
        }

        _language.EnsureValidLanguage(holder);

        UpdatedLanguages(holder);
    }

    private void OnTranslatorToggle(EntityUid translator, HandheldTranslatorComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        var hasPower = _powerCell.HasDrawCharge(translator);

        if (Transform(args.Target).ParentUid is { Valid: true } holder && EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
        {
            // This translator is held by a language speaker and thus has an intrinsic counterpart bound to it. Make sure it's up-to-date.
            var intrinsic = EntityManager.EnsureComponent<HoldsTranslatorComponent>(holder);
            var isEnabled = !component.Enabled;
            if (intrinsic.Issuer != component)
            {
                // The intrinsic comp wasn't owned by this handheld component, so this comp wasn't the active translator.
                // Thus it needs to be turned on regardless of its previous state.
                intrinsic.Issuer = component;
                isEnabled = true;
            }

            isEnabled &= hasPower;
            UpdateBoundIntrinsicComp(component, intrinsic, isEnabled);
            component.Enabled = isEnabled;
            _powerCell.SetPowerCellDrawEnabled(translator, isEnabled);

            _language.EnsureValidLanguage(holder);
            UpdatedLanguages(holder);
        }
        else
        {
            // This is a standalone translator (e.g. lying on the ground). Simply toggle its state.
            component.Enabled = !component.Enabled && hasPower;
            _powerCell.SetPowerCellDrawEnabled(translator, !component.Enabled && hasPower);
        }

        OnAppearanceChange(translator, component);

        // HasPower shows a popup when there's no power, so we do not proceed in that case
        if (hasPower)
        {
            var message =
                Loc.GetString(component.Enabled ? "translator-component-turnon" : "translator-component-shutoff", ("translator", component.Owner));
            _popup.PopupEntity(message, component.Owner, args.User);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;
        _powerCell.SetPowerCellDrawEnabled(translator, false);
        OnAppearanceChange(translator, component);

        if (Transform(translator).ParentUid is { Valid: true } holder && EntityManager.HasComponent<LanguageSpeakerComponent>(holder))
        {
            if (!EntityManager.TryGetComponent<HoldsTranslatorComponent>(holder, out var intrinsic))
                return;

            if (intrinsic.Issuer == component)
            {
                intrinsic.Enabled = false;
                EntityManager.RemoveComponent(holder, intrinsic);
            }

            _language.EnsureValidLanguage(holder);
            UpdatedLanguages(holder);
        }
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
        intrinsic.Issuer = comp;
    }

    private static void AddIfNotExists(List<string> list, string item)
    {
        if (list.Contains(item))
            return;
        list.Add(item);
    }

    private void UpdatedLanguages(EntityUid uid)
    {
        RaiseLocalEvent(uid, new SharedLanguageSystem.LanguagesUpdateEvent(), true);
    }
}
