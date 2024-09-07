using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using static Content.Shared.Paper.PaperComponent;
using Content.Shared.Timing; // Frontier
using Content.Shared.Access.Systems; // Frontier
using Content.Shared.Verbs; // Frontier
using Content.Shared.Crayon; // Frontier
using Content.Shared.Ghost; // Frontier

namespace Content.Shared.Paper;

public sealed class PaperSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!; // Frontier
    [Dependency] private readonly UseDelaySystem _useDelay = default!; // Frontier

    private const int ReapplyLimit = 10; // Frontier: limits on reapplied stamps
    private const int StampLimit = 100; // Frontier: limits on total stamps on a page (should be able to get a signature from everybody on the server on a page)

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PaperComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PaperComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PaperComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PaperComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaperComponent, PaperInputTextMessage>(OnInputTextMessage);

        SubscribeLocalEvent<ActivateOnPaperOpenedComponent, PaperWriteEvent>(OnPaperWrite);

        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(AddSignVerb); // Frontier - Sign verb hook
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
        var editable = entity.Comp.StampedBy.Count == 0 || _tagSystem.HasTag(args.Used, "WriteIgnoreStamps");
        if (_tagSystem.HasTag(args.Used, "Write") && editable)
        {
            if (entity.Comp.EditingDisabled)
            {
                var paperEditingDisabledMessage = Loc.GetString("paper-tamper-proof-modified-message");
                _popupSystem.PopupEntity(paperEditingDisabledMessage, entity, args.User);

                args.Handled = true;
                return;
            }
            var writeEvent = new PaperWriteEvent(entity, args.User);
            RaiseLocalEvent(args.Used, ref writeEvent);

            // Frontier - Restrict writing to entities with ActorComponent, players only
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            entity.Comp.Mode = PaperAction.Write;
            _uiSystem.OpenUi(entity.Owner, PaperUiKey.Key, args.User);
            UpdateUserInterface(entity);
            args.Handled = true;
            return;
        }

        // If a stamp, attempt to stamp paper
        if (TryComp<StampComponent>(args.Used, out var stampComp) &&
            !StampDelayed(args.Used)) // Frontier: check stamp is delayed, defer TryStamp
        {
            var stampInfo = GetStampInfo(stampComp); // Frontier: assign DisplayStampInfo before stamp
            if (_tagSystem.HasTag(args.Used, "Write"))
            {
                TrySign(entity, args.User, args.Used);
            }
            else if (TryStamp(entity, stampInfo, stampComp.StampState))
            { // End Frontier
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

                UpdateUserInterface(entity);

                DelayStamp(args.Used); // Frontier: prevent stamp spam
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
        if (args.Text.Length <= entity.Comp.ContentSize)
        {
            SetContent(entity, args.Text);

            if (TryComp<AppearanceComponent>(entity, out var appearance))
                _appearance.SetData(entity, PaperVisuals.Status, PaperStatus.Written, appearance);

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

    // FRONTIER - stamp precondition
    private bool CanStamp(StampDisplayInfo stampInfo, PaperComponent paperComp)
    {
        if (paperComp.StampedBy.Count >= StampLimit)
            return false;
        if (stampInfo.Reapply)
            return paperComp.StampedBy.FindAll(x => x.Equals(stampInfo)).Count < ReapplyLimit;
        else
            return !paperComp.StampedBy.Contains(stampInfo); // Original precondition
    }

    // FRONTIER - stamp reapplication: checks if a given stamp is delayed
    private bool StampDelayed(EntityUid stampUid)
    {
        return TryComp<UseDelayComponent>(stampUid, out var delay) &&
            _useDelay.IsDelayed((stampUid, delay), "stamp");
    }

    // FRONTIER - stamp reapplication: resets the delay on a given stamp
    private void DelayStamp(EntityUid stampUid)
    {
        if (TryComp<UseDelayComponent>(stampUid, out var delay))
            _useDelay.TryResetDelay(stampUid, false, delay, "stamp");
    }

    // FRONTIER - Pen signing: Adds the sign verb for pen signing
    private void AddSignVerb(EntityUid uid, PaperComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Sanity check
        if (uid != args.Target || HasComp<GhostComponent>(args.User))
            return;

        // Pens have a `Write` tag.
        if (!args.Using.HasValue || !_tagSystem.HasTag(args.Using.Value, "Write"))
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

    // FRONTIER - TrySign method, attempts to place a signature
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

            _popupSystem.PopupEntity(
                Loc.GetString(
                    "paper-component-action-signed-self",
                    ("target", paper.Owner)
                ),
                signer,
                signer
            );

            _audio.PlayPvs(paper.Comp.Sound, paper);

            _adminLogger.Add(LogType.Verb, LogImpact.Low,
                $"{ToPrettyString(signer):player} has signed {ToPrettyString(paper):paper}.");

            UpdateUserInterface(paper);

            DelayStamp(pen); // prevent stamp spam

            return true;
        }

        return false;
    }

    public void SetContent(Entity<PaperComponent> entity, string content)
    {
        entity.Comp.Content = content + '\n';
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
