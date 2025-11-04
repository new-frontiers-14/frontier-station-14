using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Timing; // Frontier
using Content.Shared.Access.Systems; // Frontier
using Content.Shared.Verbs; // Frontier
using Content.Shared.Ghost; // Frontier

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!; // Frontier

    private const int ReapplyLimit = 10; // Frontier: limits on reapplied stamps
    private const int StampLimit = 100; // Frontier: limits on total stamps on a page (should be able to get a signature from everybody on the server on a page)
    private static readonly ProtoId<TagPrototype> NFPaperStampProtectedTag = "NFPaperStampProtected"; // Frontier
    private static readonly ProtoId<TagPrototype> NFWriteIgnoreUnprotectedStampsTag = "NFWriteIgnoreUnprotectedStamps"; // Frontier

    private static readonly ProtoId<TagPrototype> WriteIgnoreStampsTag = "WriteIgnoreStamps";
    private static readonly ProtoId<TagPrototype> WriteTag = "Write";

    private EntityQuery<PaperComponent> _paperQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(AddSignVerb); // Frontier - Sign verb hook

        SubscribeLocalEvent<RandomPaperContentComponent, MapInitEvent>(OnRandomPaperContentMapInit);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);

        _paperQuery = GetEntityQuery<PaperComponent>();
    }

    private void OnMapInit(Entity<PaperComponent> entity, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(entity.Comp.Content))
        {
            SetContent(entity, Loc.GetString(entity.Comp.Content));
        }
    }

    private void OnInit(Entity<PaperComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);

        if (TryComp<AppearanceComponent>(entity, out var appearance))
        {
            if (entity.Comp.Content != "")
                _appearance.SetData(entity, PaperVisuals.Status, PaperStatus.Written, appearance);

            if (entity.Comp.StampState != null)
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
        }
    }

    private void BeforeUIOpen(Entity<PaperComponent> entity, ref BeforeActivatableUIOpenEvent args)
    {
        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnExamined(Entity<PaperComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PaperComponent)))
        {
            if (entity.Comp.Content != "")
            {
                args.PushMarkup(
                    Loc.GetString(
                        "paper-component-examine-detail-has-words",
                        ("paper", entity)
                    )
                );
            }

            if (entity.Comp.StampedBy.Count > 0)
            {
                // BEGIN FRONTIER MODIFICATION - Make stamps and signatures render separately.
                // Separate into stamps and signatures, display each name/stamp only once.
                var stamps = entity.Comp.StampedBy.FindAll(s => s.Type == StampType.RubberStamp);
                var signatures = entity.Comp.StampedBy.FindAll(s => s.Type == StampType.Signature);

                // If we have stamps, render them.
                if (stamps.Count > 0)
                {
                    var joined = string.Join(", ", stamps.Select(s => Loc.GetString(s.StampedName)).Distinct());
                    args.PushMarkup(
                        Loc.GetString(
                            "paper-component-examine-detail-stamped-by",
                            ("paper", entity.Owner),
                            ("stamps", joined)
                        )
                    );
                }

                // Ditto for signatures.
                if (signatures.Count > 0)
                {
                    var joined = string.Join(", ", signatures.Select(s => s.StampedName).Distinct());
                    args.PushMarkup(
                        Loc.GetString(
                            "paper-component-examine-detail-signed-by",
                            ("paper", entity.Owner),
                            ("stamps", joined)
                        )
                    );
                }
                // END FRONTIER MODIFICATION
            }
        }
    }

    private void OnInteractUsing(Entity<PaperComponent> entity, ref InteractUsingEvent args)
    {
        // only allow editing if there are no stamps or when using a cyberpen
        var editable = entity.Comp.StampedBy.Count == 0 || _tagSystem.HasTag(args.Used, WriteIgnoreStampsTag)
                       || _tagSystem.HasTag(args.Used, NFWriteIgnoreUnprotectedStampsTag) && !_tagSystem.HasTag(entity, NFPaperStampProtectedTag); // Frontier: protected stamps
        if (_tagSystem.HasTag(args.Used, WriteTag))
        {
            if (editable)
            {
                // Frontier - Restrict writing to entities with ActorComponent, players only
                if (!HasComp<ActorComponent>(args.User))
                {
                    args.Handled = true;
                    return;
                }
                // End Frontier

                if (entity.Comp.EditingDisabled)
                {
                    var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                    _popupSystem.PopupClient(paperEditingDisabledMessage, entity, args.User);

                    args.Handled = true;
                    return;
                }

                var ev = new PaperWriteAttemptEvent(entity.Owner);
                RaiseLocalEvent(args.User, ref ev);
                if (ev.Cancelled)
                {
                    if (ev.FailReason is not null)
                    {
                        var fileWriteMessage = Loc.GetString(ev.FailReason);
                        _popupSystem.PopupClient(fileWriteMessage, entity.Owner, args.User);
                    }

                    args.Handled = true;
                    return;
                }

                var writeEvent = new PaperWriteEvent(args.User, entity);
                RaiseLocalEvent(args.Used, ref writeEvent);

                entity.Comp.Mode = PaperAction.Write;
                _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
                UpdateUserInterface(entity);
                args.Handled = true;
                return;
            }
        }

        // If a stamp, attempt to stamp paper
        if (TryComp<StampComponent>(args.Used, out var stampComp) &&
            !StampDelayed(args.Used)) // Frontier: check stamp is delayed, defer TryStamp
        {
            // Frontier: assign DisplayStampInfo before stamp
            var stampInfo = GetStampInfo(stampComp);
            if (_tagSystem.HasTag(args.Used, WriteTag))
            {
                TrySign(entity, args.User, args.Used);
            }
            else if (TryStamp(entity, stampInfo, stampComp.StampState))
            {
                // End Frontier: assign DisplayStampInfo before stamp
                // successfully stamped, play popup
                var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                        ("user", args.User),
                        ("target", args.Target),
                        ("stamp", args.Used));

                _popupSystem.PopupEntity(stampPaperOtherMessage, args.User, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
                var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                        ("target", args.Target),
                        ("stamp", args.Used));
                _popupSystem.PopupClient(stampPaperSelfMessage, args.User, args.User);

                _audio.PlayPredicted(stampComp.Sound, entity, args.User);

                // Frontier: stamp delay and protection
                DelayStamp(args.Used);

                // Note: mode is not changed here, anyone with an open paper may still save changes.
                if (stampComp.Protected)
                    _tagSystem.AddTag(entity, NFPaperStampProtectedTag);
                // End Frontier

                UpdateUserInterface(entity);
            } // Frontier: added an indent level
        }
    }

    private static StampDisplayInfo GetStampInfo(StampComponent stamp)
    {
        return new StampDisplayInfo
        {
            Reapply = stamp.Reapply, // Frontier
            StampedName = stamp.StampedName,
            StampedColor = stamp.StampedColor
        };
    }

    private void OnInputTextMessage(Entity<PaperComponent> entity, ref PaperInputTextMessage args)
    {
        var ev = new PaperWriteAttemptEvent(entity.Owner);
        RaiseLocalEvent(args.Actor, ref ev);
        if (ev.Cancelled)
            return;

        if (args.Text.Length <= entity.Comp.ContentSize)
        {
            SetContent(entity, args.Text);

            var paperStatus = string.IsNullOrWhiteSpace(args.Text) ? PaperStatus.Blank : PaperStatus.Written;

            if (TryComp<AppearanceComponent>(entity, out var appearance))
                _appearance.SetData(entity, PaperVisuals.Status, paperStatus, appearance);

            if (TryComp(entity, out MetaDataComponent? meta))
                _metaSystem.SetEntityDescription(entity, "", meta);

            _adminLogger.Add(LogType.Chat,
                LogImpact.Low,
                $"{ToPrettyString(args.Actor):player} has written on {ToPrettyString(entity):entity} the following text: {args.Text}");

            _audio.PlayPvs(entity.Comp.Sound, entity);
        }

        entity.Comp.Mode = PaperAction.Read;
        UpdateUserInterface(entity);
    }

    private void OnRandomPaperContentMapInit(Entity<RandomPaperContentComponent> ent, ref MapInitEvent args)
    {
        if (!_paperQuery.TryComp(ent, out var paperComp))
        {
            Log.Warning($"{EntityManager.ToPrettyString(ent)} has a {nameof(RandomPaperContentComponent)} but no {nameof(PaperComponent)}!");
            RemCompDeferred(ent, ent.Comp);
            return;
        }
        var dataset = _protoMan.Index(ent.Comp.Dataset);
        // Intentionally not using the Pick overload that directly takes a LocalizedDataset,
        // because we want to get multiple attributes from the same pick.
        var pick = _random.Pick(dataset.Values);

        // Name
        _metaSystem.SetEntityName(ent, Loc.GetString(pick));
        // Description
        _metaSystem.SetEntityDescription(ent, Loc.GetString($"{pick}.desc"));
        // Content
        SetContent((ent, paperComp), Loc.GetString($"{pick}.content"));

        // Our work here is done
        RemCompDeferred(ent, ent.Comp);
    }

    private void OnPaperWrite(Entity<ActivateOnPaperOpenedComponent> entity, ref PaperWriteEvent args)
    {
        _interaction.UseInHandInteraction(args.User, entity);
    }

    /// <summary>
    ///     Accepts the name and state to be stamped onto the paper, returns true if successful.
    /// </summary>
    public bool TryStamp(Entity<PaperComponent> entity, StampDisplayInfo stampInfo, string spriteStampState)
    {
        if (CanStamp(stampInfo, entity.Comp)) // Frontier: !entity.Comp.StampedBy.Contains(stampInfo) < CanStamp(stampInfo, entity.Comp)
        {
            entity.Comp.StampedBy.Add(stampInfo);
            Dirty(entity);
            if (entity.Comp.StampState == null && TryComp<AppearanceComponent>(entity, out var appearance))
            {
                entity.Comp.StampState = spriteStampState;
                // Would be nice to be able to display multiple sprites on the paper
                // but most of the existing images overlap
                _appearance.SetData(entity, PaperVisuals.Stamp, entity.Comp.StampState, appearance);
            }
        }
        return true;
    }

    /// <summary>
    ///     Copy any stamp information from one piece of paper to another.
    /// </summary>
    public void CopyStamps(Entity<PaperComponent?> source, Entity<PaperComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        target.Comp.StampedBy = new List<StampDisplayInfo>(source.Comp.StampedBy);
        target.Comp.StampState = source.Comp.StampState;
        Dirty(target);

        // Frontier: apply stamp protection
        if (_tagSystem.HasTag(source, NFPaperStampProtectedTag))
            _tagSystem.AddTag(target, NFPaperStampProtectedTag);
        // End Frontier: apply stamp protection

        if (TryComp<AppearanceComponent>(target, out var appearance))
        {
            // delete any stamps if the stamp state is null
            _appearance.SetData(target, PaperVisuals.Stamp, target.Comp.StampState ?? "", appearance);
        }
    }

    // Frontier: stamp functions
    #region Frontier
    // stamp precondition
    private bool CanStamp(StampDisplayInfo stampInfo, PaperComponent paperComp)
    {
        if (paperComp.StampedBy.Count >= StampLimit)
            return false;
        if (stampInfo.Reapply)
            return paperComp.StampedBy.FindAll(x => x.Equals(stampInfo)).Count < ReapplyLimit;
        else
            return !paperComp.StampedBy.Contains(stampInfo); // Original precondition
    }

    // stamp reapplication: checks if a given stamp is delayed
    private bool StampDelayed(EntityUid stampUid)
    {
        return TryComp<UseDelayComponent>(stampUid, out var delay) &&
            _useDelay.IsDelayed((stampUid, delay), "stamp");
    }

    // stamp reapplication: resets the delay on a given stamp
    private void DelayStamp(EntityUid stampUid)
    {
        if (TryComp<UseDelayComponent>(stampUid, out var delay))
            _useDelay.TryResetDelay(stampUid, false, delay, "stamp");
    }

    // Pen signing: Adds the sign verb for pen signing
    private void AddSignVerb(EntityUid uid, PaperComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Sanity check
        if (uid != args.Target || HasComp<GhostComponent>(args.User))
            return;

        // Pens have a `Write` tag.
        if (!args.Using.HasValue || !_tagSystem.HasTag(args.Using.Value, WriteTag))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TrySign((uid, component), args.User, args.Using.Value);
            },
            Text = Loc.GetString("paper-component-verb-sign")
            // Icon = Don't have an icon yet. Todo for later.
        };
        args.Verbs.Add(verb);
    }

    // TrySign method, attempts to place a signature
    public bool TrySign(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        if (!TryComp<StampComponent>(pen, out var stamp))
            return false;

        // Generate display information.
        var info = GetStampInfo(stamp);
        info.Type = StampType.Signature;
        info.StampedName = Name(signer);

        // Try stamp with the info, return false if failed.
        if (!StampDelayed(pen) && TryStamp(paper, info, "paper_stamp-nf-signature"))
        {
            // Signing successful, popup time.
            _popupSystem.PopupEntity(
                Loc.GetString(
                    "paper-component-action-signed-other",
                    ("user", signer),
                    ("target", paper.Owner)
                ),
                signer,
                Filter.PvsExcept(signer, entityManager: EntityManager),
                true
            );

            _popupSystem.PopupClient(
                Loc.GetString(
                    "paper-component-action-signed-self",
                    ("target", paper.Owner)
                ),
                signer,
                signer
            );

            _audio.PlayPredicted(paper.Comp.Sound, paper, signer);

            _adminLogger.Add(LogType.Verb, LogImpact.Low,
                $"{ToPrettyString(signer):player} has signed {ToPrettyString(paper):paper}.");

            UpdateUserInterface(paper);

            DelayStamp(pen); // prevent stamp spam

            return true;
        }

        return false;
    }
    #endregion Frontier
    // End Frontier

    public void SetContent(EntityUid entity, string content)
    {
        if (!TryComp<PaperComponent>(entity, out var paper))
            return;
        SetContent((entity, paper), content);
    }

    public void SetContent(Entity<PaperComponent> entity, string content)
    {
        entity.Comp.Content = content;
        Dirty(entity);
        UpdateUserInterface(entity);

        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var status = string.IsNullOrWhiteSpace(content)
            ? PaperStatus.Blank
            : PaperStatus.Written;

        _appearance.SetData(entity, PaperVisuals.Status, status, appearance);
    }

    private void UpdateUserInterface(Entity<PaperComponent> entity)
    {
        _uiSystem.SetUiState(entity.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(entity.Comp.Content, entity.Comp.StampedBy, entity.Comp.Mode));
    }
}

/// <summary>
/// Event fired when using a pen on paper, opening the UI.
/// </summary>
[ByRefEvent]
public record struct PaperWriteEvent(EntityUid User, EntityUid Paper);

/// <summary>
/// Cancellable event for attempting to write on a piece of paper.
/// </summary>
/// <param name="paper">The paper that the writing will take place on.</param>
[ByRefEvent]
public record struct PaperWriteAttemptEvent(EntityUid Paper, string? FailReason = null, bool Cancelled = false);
