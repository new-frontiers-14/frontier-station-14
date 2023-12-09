using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Content.Shared.Mobs.Components;

namespace Content.Server.Language;

public sealed class TranslatorImplanterSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TranslatorImplanterComponent, AfterInteractEvent>(OnImplant);
    }

    private void OnImplant(EntityUid implanter, TranslatorImplanterComponent component, AfterInteractEvent args)
    {
        if (component.Used || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<LanguageSpeakerComponent>(args.Target, out var speaker))
            return;

        if (component.MobsOnly && !HasComp<MobStateComponent>(args.Target))
        {
            _popup.PopupEntity("translator-implanter-refuse", component.Owner);
            return;
        }

        var (_, understood) = _language.GetAllLanguages(args.Target.Value);
        if (!component.RequiredLanguages.Any(lang => understood.Contains(lang)))
        {
            RefusesPopup(implanter, args.Target.Value);
            return;
        }

        var intrinsic = EnsureComp<IntrinsicTranslatorComponent>(args.Target.Value);
        foreach (var lang in component.SpokenLanguages)
        {
            if (!intrinsic.SpokenLanguages.Contains(lang))
                intrinsic.SpokenLanguages.Add(lang);
        }

        foreach (var lang in component.UnderstoodLanguages)
        {
            if (!intrinsic.UnderstoodLanguages.Contains(lang))
                intrinsic.UnderstoodLanguages.Add(lang);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} used {ToPrettyString(implanter):implanter} to give {ToPrettyString(args.Target.Value):target} the following languages:"
            + $"\nSpoken: {string.Join(", ", component.SpokenLanguages)}; Understood: {string.Join(", ", component.UnderstoodLanguages)}");
    }

    private void RefusesPopup(EntityUid implanter, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("translator-implanter-refuse", ("target", target), ("implanter", implanter)), implanter);
    }
}
