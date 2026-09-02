using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._NF.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;
using Content.Server.Hands.Systems;
using Content.Server.Medical.Components;
using Content.Shared._NF.Medical;
using Content.Shared.CartridgeLoader;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._NF.Medical;

public sealed class HealthAnalyzerPrinterSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    private static readonly Regex TemplateInsert = new(@"\{([\w.]+)\}");

    public override void Initialize()
    {
        SubscribeLocalEvent<MedTekCartridgePrinterComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<MedTekCartridgePrinterComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);

        SubscribeLocalEvent<HealthAnalyzerPrinterComponent, HealthAnalyzerPrintPatientRecordMessage>(OnPrint);
    }

    // Cartridge handling
    private void OnCartridgeAdded(Entity<MedTekCartridgePrinterComponent> ent, ref CartridgeAddedEvent args)
    {
        // We're cloning some settings from the cartridge to the PDA, because that way it's easier to retrieve later
        EnsureComp<HealthAnalyzerPrinterComponent>(args.Loader).PrintTemplate = ent.Comp.PrintTemplate;
    }

    private void OnCartridgeRemoved(Entity<MedTekCartridgePrinterComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (_cartridgeLoader.TryGetProgram<MedTekCartridgePrinterComponent>(args.Loader, out _, out var program))
        {
            // If someone has, for whatever reason, added more than one variant of the MedTek printer, arbitrarily
            // choose the template of any of them
            HealthAnalyzerPrinterComponent? printer = null;
            if (Resolve(args.Loader, ref printer))
            {
                printer.PrintTemplate = program.PrintTemplate;
            }
        }
        else
        {
            // After the last MedTek cartridge with printer support has been removed, we don't need the settings
            // component anymore.
            RemComp<HealthAnalyzerPrinterComponent>(args.Loader);
        }
    }

    // Printing
    private void OnPrint(Entity<HealthAnalyzerPrinterComponent> entity, ref HealthAnalyzerPrintPatientRecordMessage args)
    {
        var printer = entity.Comp;
        // Prevent users from printing too quickly
        if (printer.PrintAllowedAfter >= _gameTiming.CurTime)
        {
            return;
        }

        HealthAnalyzerComponent? analyzer = null;
        if (!Resolve(entity.Owner, ref analyzer))
        {
            return;
        }

        // The health analyzer UI disables the button when the patient is invalid or out of range
        if (analyzer.ScannedEntity is not { Valid: true } patient)
        {
            return;
        }

        var user = args.Actor;
        if (!IsInRange(patient, user, analyzer.MaxScanRange))
        {
            return;
        }

        // Create slip of paper according to template
        var paper = Spawn(printer.PrintTemplate, Transform(user).Coordinates);
        ComposePatientRecord(paper, user, patient);
        _label.Label(paper, GetEntityName(patient));
        _hands.PickupOrDrop(user, paper);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/printer.ogg"), user);

        // Start cooldown
        printer.PrintAllowedAfter = _gameTiming.CurTime + printer.PrintCooldown;
    }

    private bool IsInRange(EntityUid patient, EntityUid user, float? maxScanRange)
    {
        if (maxScanRange == null)
        {
            return true;
        }

        return _transform.InRange(
            (patient, Transform(patient)),
            (user, Transform(user)),
            maxScanRange.Value
        );
    }

    private void ComposePatientRecord(EntityUid uid, EntityUid responder, EntityUid patient)
    {
        PaperComponent? paper = null;
        DamageableComponent? damageable = null;
        if (!Resolve(uid, ref paper) || !Resolve(patient, ref damageable))
        {
            return;
        }

        var template = paper.Content;

        // Anything in this dictionary can be interpolated into the print template
        Dictionary<string, Func<string>> inserts = new()
        {
            { "patient.name", () => GetEntityName(patient) },
            { "patient.species", () => GetEntitySpecies(patient) },
            { "responder.name", () => GetEntityName(responder) },
            { "roundTime", () => (_gameTiming.CurTime - _gameTicker.RoundStartTimeSpan).ToString(@"hh\:mm") },
            { "damageList", () => ComposeDamageList(damageable) },
        };

        var content = TemplateInsert.Replace(template,
            match =>
            {
                var key = match.Groups[1].Value;
                if (inserts.TryGetValue(key, out var value))
                {
                    return value.Invoke();
                }

                return match.Value;
            });

        _paper.SetContent((uid, paper), content);
    }

    private string ComposeDamageList(DamageableComponent damageable)
    {
        if (damageable.TotalDamage <= 0)
        {
            return Loc.GetString("health-analyzer-printout-damage-none");
        }

        var report = new FormattedMessage();
        var groups = damageable.DamagePerGroup.OrderByDescending(entry => entry.Value);
        var damage = damageable.Damage.DamageDict;
        foreach (var (groupId, groupDamage) in groups)
        {
            if (groupDamage <= 0)
            {
                continue;
            }

            var group = _prototypes.Index<DamageGroupPrototype>(groupId);

            // Group header
            var groupTitleText = Loc.GetString(
                "health-analyzer-printout-damage-group-text",
                ("damageGroup", group.LocalizedName),
                ("amount", groupDamage)
            );
            report.AddText(groupTitleText);
            report.PushNewline();

            // List individual damage types
            foreach (var type in group.DamageTypes)
            {
                var amount = damage.GetValueOrDefault(type, 0);
                if (amount <= 0)
                {
                    continue;
                }

                report.AddText(Loc.GetString(
                    "health-analyzer-printout-damage-type-text",
                    ("damageType", _prototypes.Index<DamageTypePrototype>(type).LocalizedName),
                    ("amount", amount)
                ));
                report.PushNewline();
            }
        }

        return report.ToMarkup();
    }

    private string GetEntityName(EntityUid uid)
    {
        return HasComp<MetaDataComponent>(uid)
            ? Identity.Name(uid, EntityManager)
            : Loc.GetString("health-analyzer-window-entity-unknown-text");
    }

    private string GetEntitySpecies(EntityUid uid)
    {
        return Loc.GetString(
            TryComp<HumanoidAppearanceComponent>(uid, out var appearance)
                ? _prototypes.Index(appearance.Species).Name
                : "health-analyzer-window-entity-unknown-species-text"
        );
    }
}
