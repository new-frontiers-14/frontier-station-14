using System.Linq;
using System.Text;
using Content.Server.Popups;
using Content.Server.Stack; // Frontier
using Content.Server._NF.Smuggling; // Frontier
using Content.Server._NF.Smuggling.Components; // Frontier
using Content.Server.Radio.EntitySystems; // Frontier
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Fluids.Components;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Content.Shared.Stacks; // Frontier
using Content.Shared.Radio; // Frontier
using Content.Shared.Access.Systems; // Frontier
using Robust.Shared.Prototypes; // Frontier
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Containers.ItemSlots; // Frontier

// todo: remove this stinky LINQy

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!; // Frontier
        [Dependency] private readonly SharedAudioSystem _audio = default!; // Frontier
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Frontier
        [Dependency] private readonly RadioSystem _radio = default!; // Frontier
        [Dependency] private readonly AccessReaderSystem _accessReader = default!; // Frontier
        [Dependency] private readonly DeadDropSystem _deadDrop = default!; // Frontier
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!; // Frontier

        private readonly List<EntityUid> _scannedDeadDrops = []; // Frontier
        private int _amountScanned = 0; // Frontier

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        }

        // Frontier: add dead drop rewards
        /// <summary>
        ///     Gives rewards in form of FUC to the detective for interacting with the dead drop system
        /// </summary>
        private void GiveReward(EntityUid uidOrigin, EntityUid target, int amount, string msg)
        {
            SoundSpecifier confirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
            _audio.PlayPvs(_audio.GetSound(confirmSound), uidOrigin);

            var stackPrototype = _prototypeManager.Index<StackPrototype>("FrontierUplinkCoin");
            _stackSystem.Spawn(amount, stackPrototype, Transform(target).Coordinates);

            var channel = _prototypeManager.Index<RadioChannelPrototype>("Nfsd");
            _radio.SendRadioMessage(uidOrigin, msg, channel, uidOrigin);
        }
        // Frontier: add dead drop rewards

        private void UpdateUserInterface(EntityUid uid, ForensicScannerComponent component)
        {
            var state = new ForensicScannerBoundUserInterfaceState(
                component.Fingerprints,
                component.Fibers,
                component.TouchDNAs,
                component.SolutionDNAs,
                component.Residues,
                component.LastScannedName,
                component.PrintCooldown,
                component.PrintReadyAt);

            _uiSystem.SetUiState(uid, ForensicScannerUiKey.Key, state);
        }

        private void OnDoAfter(EntityUid uid, ForensicScannerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out ForensicScannerComponent? scanner))
                return;

            if (args.Args.Target != null)
            {
                if (!TryComp<ForensicsComponent>(args.Args.Target, out var forensics))
                {
                    scanner.Fingerprints = new();
                    scanner.Fibers = new();
                    scanner.TouchDNAs = new();
                    scanner.Residues = new();
                }
                else
                {
                    scanner.Fingerprints = forensics.Fingerprints.ToList();
                    scanner.Fibers = forensics.Fibers.ToList();
                    scanner.TouchDNAs = forensics.DNAs.ToList();
                    scanner.Residues = forensics.Residues.ToList();
                }

                // Frontier: contraband poster/pod scanning
                if (_itemSlots.TryGetSlot(uid, "forensics_cartridge", out var itemSlot) && itemSlot.HasItem)
                {
                    EntityUid posterUid = args.Args.Target.Value;
                    // Prints FUC if you've successfully scanned a dead drop poster that has spawned a dead drop at least once
                    if (HasComp<DeadDropComponent>(posterUid))
                    {
                        if (TryComp<DeadDropComponent>(posterUid, out var deadDropComponent) &&
                            deadDropComponent.DeadDropCalled)
                        {
                            var amount = 3;
                            var msg = Loc.GetString("forensic-reward-poster", ("amount", amount));

                            GiveReward(uid, posterUid, amount, msg);

                            // Makes the boolean true so you can't just keep scanning the same poster over and over again
                            _deadDrop.CompromiseDeadDrop(posterUid, deadDropComponent);
                        }
                    }
                    else if (MetaData(Transform(posterUid).ParentUid).EntityName.Equals("Syndicate Supply Drop") &&
                            !_scannedDeadDrops.Contains(Transform(posterUid).ParentUid) && _amountScanned <= 5)
                    {
                        // Only works if you scan anything on the Syndicate Supply Drop and have scanned less than 5 times total
                        var amount = 6;
                        var msg = Loc.GetString("forensic-reward-drop", ("amount", amount));

                        GiveReward(uid, posterUid, amount, msg);

                        _scannedDeadDrops.Add(Transform(posterUid).ParentUid);
                        _amountScanned++;
                    }
                }
                // End Frontier: contraband poster/pod scanning

                if (_tag.HasTag(args.Args.Target.Value, "DNASolutionScannable"))
                {
                    scanner.SolutionDNAs = _forensicsSystem.GetSolutionsDNA(args.Args.Target.Value);
                } else
                {
                    scanner.SolutionDNAs = new();
                }

                scanner.LastScannedName = MetaData(args.Args.Target.Value).EntityName;
            }

            OpenUserInterface(args.Args.User, (uid, scanner));
        }

        /// <remarks>
        /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
        /// </remarks>
        private void StartScan(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.ScanDelay, new ForensicScannerDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true
            });
        }

        private void OnUtilityVerb(EntityUid uid, ForensicScannerComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || component.CancelToken != null)
                return;

            var verb = new UtilityVerb()
            {
                Act = () => StartScan(uid, component, args.User, args.Target),
                IconEntity = GetNetEntity(uid),
                Text = Loc.GetString("forensic-scanner-verb-text"),
                Message = Loc.GetString("forensic-scanner-verb-message")
            };

            args.Verbs.Add(verb);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
                return;

            StartScan(uid, component, args.User, args.Target.Value);
        }

        private void OnAfterInteractUsing(EntityUid uid, ForensicScannerComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
                return;

            foreach (var fiber in component.Fibers)
            {
                if (fiber == pad.Sample)
                {
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), uid, args.User);
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), uid, args.User);
                    return;
                }
            }

            _audioSystem.PlayPvs(component.SoundNoMatch, uid);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), uid, args.User);
        }

        private void OnBeforeActivatableUIOpen(EntityUid uid, ForensicScannerComponent component, BeforeActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
        {
            UpdateUserInterface(scanner, scanner.Comp);

            _uiSystem.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user);
        }

        private void OnPrint(EntityUid uid, ForensicScannerComponent component, ForensicScannerPrintMessage args)
        {
            var user = args.Actor;

            if (_gameTiming.CurTime < component.PrintReadyAt)
            {
                // This shouldn't occur due to the UI guarding against it, but
                // if it does, tell the user why nothing happened.
                _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), uid, user);
                return;
            }

            // Spawn a piece of paper.
            var printed = EntityManager.SpawnEntity(component.MachineOutput, Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paperComp))
            {
                Log.Error("Printed paper did not have PaperComponent.");
                return;
            }

            _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScannedName)));

            var text = new StringBuilder();

            text.AppendLine(Loc.GetString("forensic-scanner-interface-fingerprints"));
            foreach (var fingerprint in component.Fingerprints)
            {
                text.AppendLine(fingerprint);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-fibers"));
            foreach (var fiber in component.Fibers)
            {
                text.AppendLine(fiber);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-dnas"));
            foreach (var dna in component.TouchDNAs)
            {
                text.AppendLine(dna);
            }
            foreach (var dna in component.SolutionDNAs)
            {
                Log.Debug(dna);
                if (component.TouchDNAs.Contains(dna))
                    continue;
                text.AppendLine(dna);
            }
            text.AppendLine();
            text.AppendLine(Loc.GetString("forensic-scanner-interface-residues"));
            foreach (var residue in component.Residues)
            {
                text.AppendLine(residue);
            }

            _paperSystem.SetContent((printed, paperComp), text.ToString());
            _audioSystem.PlayPvs(component.SoundPrint, uid,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
        }

        private void OnClear(EntityUid uid, ForensicScannerComponent component, ForensicScannerClearMessage args)
        {
            component.Fingerprints = new();
            component.Fibers = new();
            component.TouchDNAs = new();
            component.SolutionDNAs = new();
            component.LastScannedName = string.Empty;

            UpdateUserInterface(uid, component);
        }
    }
}
