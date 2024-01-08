using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Content.Shared.Language.Systems;
using Content.Shared.Mobs.Components;

namespace Content.Server.Language;

public sealed class TranslatorImplanterSystem : SharedTranslatorImplanterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TranslatorImplanterComponent, AfterInteractEvent>(OnImplant);
    }

    private void OnImplant(EntityUid implanter, TranslatorImplanterComponent component, AfterInteractEvent args)
    {
        if (component.Used || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!TryComp<LanguageSpeakerComponent>(target, out var speaker))
            return;

        if (component.MobsOnly && !HasComp<MobStateComponent>(target))
        {
            _popup.PopupEntity("translator-implanter-refuse", component.Owner);
            return;
        }

        var (_, understood) = _language.GetAllLanguages(target);
        if (component.RequiredLanguages.Count > 0 && !component.RequiredLanguages.Any(lang => understood.Contains(lang)))
        {
            RefusesPopup(implanter, target);
            return;
        }

        var intrinsic = EnsureComp<ImplantedTranslatorComponent>(target);
        intrinsic.Enabled = true;

        foreach (var lang in component.SpokenLanguages.Where(lang => !intrinsic.SpokenLanguages.Contains(lang)))
            intrinsic.SpokenLanguages.Add(lang);

        foreach (var lang in component.UnderstoodLanguages.Where(lang => !intrinsic.UnderstoodLanguages.Contains(lang)))
            intrinsic.UnderstoodLanguages.Add(lang);

        component.Used = true;
        SuccessPopup(implanter, target);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} used {ToPrettyString(implanter):implanter} to give {ToPrettyString(target):target} the following languages:"
            + $"\nSpoken: {string.Join(", ", component.SpokenLanguages)}; Understood: {string.Join(", ", component.UnderstoodLanguages)}");

        OnAppearanceChange(implanter, component);
        RaiseLocalEvent(target, new SharedLanguageSystem.LanguagesUpdateEvent(), true);
    }

    private void RefusesPopup(EntityUid implanter, EntityUid target)
    {
        _popup.PopupEntity(
            Loc.GetString("translator-implanter-refuse", ("implanter", implanter), ("target", target)),
            implanter);
    }

    private void SuccessPopup(EntityUid implanter, EntityUid target)
    {
        _popup.PopupEntity(
            Loc.GetString("translator-implanter-success", ("implanter", implanter), ("target", target)),
            implanter);
    }
}
