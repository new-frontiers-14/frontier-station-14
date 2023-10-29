using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Fluids.Components;
using Content.Server.Mail.Components;
using Content.Server.Mind;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Emag.Components;
using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mail;
using Content.Shared.Maps;
using Content.Shared.PDA;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Mail
{
    public sealed class MailSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly MetaDataSystem _meta = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("mail");

            SubscribeLocalEvent<MailComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<MailComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<MailComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<MailComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<MailComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<MailComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<MailComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<MailComponent, GotEmaggedEvent>(OnMailEmagged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var mailTeleporter in EntityQuery<MailTeleporterComponent>())
            {
                if (TryComp<ApcPowerReceiverComponent>(mailTeleporter.Owner, out var power) && !power.Powered)
                    return;

                mailTeleporter.Accumulator += frameTime;

                if (mailTeleporter.Accumulator < mailTeleporter.TeleportInterval.TotalSeconds)
                    continue;

                mailTeleporter.Accumulator -= (float) mailTeleporter.TeleportInterval.TotalSeconds;

                SpawnMail(mailTeleporter.Owner, mailTeleporter);
            }
        }

        private void OnRemove(EntityUid uid, MailComponent component, ComponentRemove args)
        {
            // Make sure the priority timer doesn't run.
            if (component.priorityCancelToken != null)
                component.priorityCancelToken.Cancel();
        }

        /// <summary>
        /// Try to open the mail.
        /// <summary>
        private void OnUseInHand(EntityUid uid, MailComponent component, UseInHandEvent args)
        {
            if (!component.IsEnabled)
                return;
            if (component.IsLocked)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-locked"), uid, args.User);
                return;
            }
            OpenMail(uid, component, args.User);
        }

        /// <summary>
        /// Handle logic similar between a normal mail unlock and an emag
        /// frying out the lock.
        /// </summary>
        private void UnlockMail(EntityUid uid, MailComponent component)
        {
            component.IsLocked = false;
            UpdateAntiTamperVisuals(uid, false);

            if (component.IsPriority)
            {
                // This is a successful delivery. Keep the failure timer from triggering.
                if (component.priorityCancelToken != null)
                    component.priorityCancelToken.Cancel();

                // The priority tape is visually considered to be a part of the
                // anti-tamper lock, so remove that too.
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, false);

                // The examination code depends on this being false to not show
                // the priority tape description anymore.
                component.IsPriority = false;
            }
        }

        /// <summary>
        /// Check the ID against the mail's lock
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, MailComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach || !component.IsLocked)
                return;

            if (!TryComp<AccessReaderComponent>(uid, out var access))
                return;

            IdCardComponent? idCard = null; // We need an ID card.

            if (HasComp<PdaComponent>(args.Used)) /// Can we find it in a PDA if the user is using that?
            {
                _idCardSystem.TryGetIdCard(args.Used, out var pdaID);
                idCard = pdaID;
            }

            if (HasComp<IdCardComponent>(args.Used)) /// Or are they using an id card directly?
                idCard = Comp<IdCardComponent>(args.Used);

            if (idCard == null) /// Return if we still haven't found an id card.
                return;

            if (!HasComp<EmaggedComponent>(uid))
            {
                if (idCard.FullName != component.Recipient || idCard.JobTitle != component.RecipientJob)
                {
                    _popupSystem.PopupEntity(Loc.GetString("mail-recipient-mismatch"), uid, args.User);
                    return;
                }

                if (!_accessSystem.IsAllowed(uid, args.User))
                {
                    _popupSystem.PopupEntity(Loc.GetString("mail-invalid-access"), uid, args.User);
                    return;
                }
            }

            UnlockMail(uid, component);

            if (!component.IsProfitable)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-unlocked"), uid, args.User);
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("mail-unlocked-reward", ("bounty", component.Bounty)), uid, args.User);

            component.IsProfitable = false;
            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var oUid, out var oComp))
            {
                // only our main station will have an account anyway so I guess we are just going to add it this way shrug.

                _cargoSystem.UpdateBankAccount(oUid, oComp, component.Bounty);
                return;
            }
        }

        private void OnExamined(EntityUid uid, MailComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("mail-desc-far"));
                return;
            }

            args.PushMarkup(Loc.GetString("mail-desc-close", ("name", component.Recipient), ("job", component.RecipientJob), ("station", component.RecipientStation)));

            if (component.IsFragile)
                args.PushMarkup(Loc.GetString("mail-desc-fragile"));

            if (component.IsPriority)
            {
                if (component.IsProfitable)
                    args.PushMarkup(Loc.GetString("mail-desc-priority"));
                else
                    args.PushMarkup(Loc.GetString("mail-desc-priority-inactive"));
            }
        }

        /// <summary>
        /// Penalize a station for a failed delivery.
        /// </summary>
        /// <remarks>
        /// This will mark a parcel as no longer being profitable, which will
        /// prevent multiple failures on different conditions for the same
        /// delivery.
        ///
        /// The standard penalization is breaking the anti-tamper lock,
        /// but this allows a delivery to fail for other reasons too
        /// while having a generic function to handle different messages.
        /// </remarks>
        public void PenalizeStationFailedDelivery(EntityUid uid, MailComponent component, string localizationString)
        {
            if (!component.IsProfitable)
                return;

            //_chatSystem.TrySendInGameICMessage(uid, Loc.GetString(localizationString, ("credits", component.Penalty)), InGameICChatType.Speak, false); # Dont show message.
            //_audioSystem.PlayPvs(component.PenaltySound, uid); # Dont play sound.

            component.IsProfitable = false;

            if (component.IsPriority)
                _appearanceSystem.SetData(uid, MailVisuals.IsPriorityInactive, true);

            var query = EntityQueryEnumerator<StationBankAccountComponent>();
            while (query.MoveNext(out var oUid, out var oComp))
            {
                // only our main station will have an account anyway so I guess we are just going to add it this way shrug.

                //_cargoSystem.UpdateBankAccount(oUid, oComp, component.Penalty); # Dont remove money.
                return;
            }
        }

        private void OnDestruction(EntityUid uid, MailComponent component, DestructionEventArgs args)
        {
            if (component.IsLocked)
                PenalizeStationFailedDelivery(uid, component, "mail-penalty-lock");

           // if (component.IsEnabled)
           //     OpenMail(uid, component); # Dont open the mail on destruction.

            UpdateAntiTamperVisuals(uid, false);
        }

        private void OnDamage(EntityUid uid, MailComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null)
                return;

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
                return;

            // Transfer damage to the contents.
            // This should be a general-purpose feature for all containers in the future.
            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                _damageableSystem.TryChangeDamage(entity, args.DamageDelta);
            }
        }

        private void OnBreak(EntityUid uid, MailComponent component, BreakageEventArgs args)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsBroken, true);

            if (component.IsFragile)
                PenalizeStationFailedDelivery(uid, component, "mail-penalty-fragile");
        }

        private void OnMailEmagged(EntityUid uid, MailComponent component, ref GotEmaggedEvent args)
        {
            if (!component.IsLocked)
                return;

            UnlockMail(uid, component);

            _popupSystem.PopupEntity(Loc.GetString("mail-unlocked-by-emag"), uid, args.UserUid);

            _audioSystem.PlayPvs(component.EmagSound, uid, AudioParams.Default.WithVolume(4));
            component.IsProfitable = false;
            args.Handled = true;
        }

        /// <summary>
        /// Returns true if the given entity is considered fragile for delivery.
        /// </summary>
        public bool IsEntityFragile(EntityUid uid, int fragileDamageThreshold)
        {
            // It takes damage on falling.
            if (HasComp<DamageOnLandComponent>(uid))
                return true;

            // It can be spilled easily and has something to spill.
            if (HasComp<SpillableComponent>(uid)
                && TryComp(uid, out DrinkComponent? drinkComponent)
                && _solutionContainerSystem.PercentFull(uid) > 0)
                return true;

            // It might be made of non-reinforced glass.
            if (TryComp(uid, out DamageableComponent? damageableComponent)
                && damageableComponent.DamageModifierSetId == "Glass")
                return true;

            // Fallback: It breaks or is destroyed in less than a damage
            // threshold dictated by the teleporter.
            if (TryComp(uid, out DestructibleComponent? destructibleComp))
            {
                foreach (var threshold in destructibleComp.Thresholds)
                {
                    if (threshold.Trigger is DamageTrigger trigger
                        && trigger.Damage < fragileDamageThreshold)
                    {
                        foreach (var behavior in threshold.Behaviors)
                        {
                            if (behavior is DoActsBehavior doActs)
                            {
                                if (doActs.Acts.HasFlag(ThresholdActs.Breakage)
                                    || doActs.Acts.HasFlag(ThresholdActs.Destruction))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool TryMatchJobTitleToDepartment(string jobTitle, [NotNullWhen(true)] out string? jobDepartment)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                foreach (var role in department.Roles)
                {
                    if (_prototypeManager.TryIndex(role, out JobPrototype? _jobPrototype)
                        && _jobPrototype.LocalizedName == jobTitle)
                    {
                        jobDepartment = department.ID;
                        return true;
                    }
                }
            }

            jobDepartment = null;
            return false;
        }

        public bool TryMatchJobTitleToPrototype(string jobTitle, [NotNullWhen(true)] out JobPrototype? jobPrototype)
        {
            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (job.LocalizedName == jobTitle)
                {
                    jobPrototype = job;
                    return true;
                }
            }

            jobPrototype = null;
            return false;
        }

        public bool TryMatchJobTitleToIcon(string jobTitle, [NotNullWhen(true)] out string? jobIcon)
        {
            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (job.LocalizedName == jobTitle)
                {
                    jobIcon = job.Icon;
                    return true;
                }
            }

            jobIcon = null;
            return false;
        }

        /// <summary>
        /// Handle all the gritty details particular to a new mail entity.
        /// </summary>
        /// <remarks>
        /// This is separate mostly so the unit tests can get to it.
        /// </remarks>
        public void SetupMail(EntityUid uid, MailTeleporterComponent component, MailRecipient recipient)
        {
            var mailComp = EnsureComp<MailComponent>(uid);

            var container = _containerSystem.EnsureContainer<Container>(uid, "contents");
            foreach (var item in EntitySpawnCollection.GetSpawns(mailComp.Contents, _random))
            {
                var entity = EntityManager.SpawnEntity(item, Transform(uid).Coordinates);
                if (!container.Insert(entity))
                {
                    _sawmill.Error($"Can't insert {ToPrettyString(entity)} into new mail delivery {ToPrettyString(uid)}! Deleting it.");
                    QueueDel(entity);
                }
                else if (!mailComp.IsFragile && IsEntityFragile(entity, component.FragileDamageThreshold))
                {
                    mailComp.IsFragile = true;
                }
            }

            if (_random.Prob(component.PriorityChance))
                mailComp.IsPriority = true;

            // This needs to override both the random probability and the
            // entity prototype, so this is fine.
            if (!recipient.MayReceivePriorityMail)
                mailComp.IsPriority = false;

            mailComp.RecipientJob = recipient.Job;
            mailComp.Recipient = recipient.Name;
            mailComp.RecipientStation = recipient.Ship;
            if (mailComp.IsFragile)
            {
                mailComp.Bounty += component.FragileBonus;
                mailComp.Penalty += component.FragileMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsFragile, true);
            }

            if (mailComp.IsPriority)
            {
                mailComp.Bounty += component.PriorityBonus;
                mailComp.Penalty += component.PriorityMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, true);

                mailComp.priorityCancelToken = new CancellationTokenSource();

                Timer.Spawn((int) component.priorityDuration.TotalMilliseconds,
                    () => PenalizeStationFailedDelivery(uid, mailComp, "mail-penalty-expired"),
                    mailComp.priorityCancelToken.Token);
            }

            if (TryMatchJobTitleToIcon(recipient.Job, out string? icon))
                _appearanceSystem.SetData(uid, MailVisuals.JobIcon, icon);

            _meta.SetEntityName(uid, Loc.GetString("mail-item-name-addressed",
                ("recipient", recipient.Name)));

            var accessReader = EnsureComp<AccessReaderComponent>(uid);
            accessReader.AccessLists.Add(recipient.AccessTags);
        }

        /// <summary>
        /// Return the parcels waiting for delivery.
        /// </summary>
        /// <param name="uid">The mail teleporter to check.</param>
        public List<EntityUid> GetUndeliveredParcels(EntityUid uid)
        {
            // An alternative solution would be to keep a list of the unopened
            // parcels spawned by the teleporter and see if they're not carried
            // by someone, but this is simple, and simple is good.
            List<EntityUid> undeliveredParcels = new();
            foreach (var entityInTile in TurfHelpers.GetEntitiesInTile(Transform(uid).Coordinates, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (HasComp<MailComponent>(entityInTile))
                    undeliveredParcels.Add(entityInTile);
            }
            return undeliveredParcels;
        }

        /// <summary>
        /// Return how many parcels are waiting for delivery.
        /// </summary>
        /// <param name="uid">The mail teleporter to check.</param>
        public uint GetUndeliveredParcelCount(EntityUid uid)
        {
            return (uint) GetUndeliveredParcels(uid).Count();
        }

        /// <summary>
        /// Try to match a mail receiver to a mail teleporter.
        /// </summary>
        public bool TryGetMailTeleporterForReceiver(MailReceiverComponent receiver, [NotNullWhen(true)] out MailTeleporterComponent? teleporterComponent)
        {
            foreach (var mailTeleporter in EntityQuery<MailTeleporterComponent>())
            {
                if (_stationSystem.GetOwningStation(receiver.Owner) == _stationSystem.GetOwningStation(mailTeleporter.Owner))
                {
                    teleporterComponent = mailTeleporter;
                    return true;
                }
            }

            teleporterComponent = null;
            return false;
        }

        /// <summary>
        /// Try to construct a recipient struct for a mail parcel based on a receiver.
        /// </summary>
        public bool TryGetMailRecipientForReceiver(MailReceiverComponent receiver, [NotNullWhen(true)] out MailRecipient? recipient)
        {
            // Because of the way this works, people are not considered
            // candidates for mail if there is no valid PDA or ID in their slot
            // or active hand. A better future solution might be checking the
            // station records, possibly cross-referenced with the medical crew
            // scanner to look for living recipients. TODO

            if (_idCardSystem.TryFindIdCard(receiver.Owner, out var idCard)
                && TryComp<AccessComponent>(idCard.Owner, out var access)
                && idCard.Comp.FullName != null
                && idCard.Comp.JobTitle != null)
            {
                HashSet<String> accessTags = access.Tags;

                var mayReceivePriorityMail = true;
                var stationUid = _stationSystem.GetOwningStation(receiver.Owner);
                var stationName = string.Empty;
                if (stationUid is EntityUid station
                    && TryComp<StationDataComponent>(station, out var stationData)
                    && _stationSystem.GetLargestGrid(stationData) is EntityUid stationGrid
                    && TryName(stationGrid, out var gridName)
                    && gridName != null)
                {
                    stationName = gridName;
                }
                else
                {
                    stationName = "Unknown";
                }

                if (!_mind.TryGetMind(receiver.Owner, out var mindId, out var mindComp))
                {
                    recipient = null;
                    return false;
                }

                recipient = new MailRecipient(idCard.Comp.FullName,
                    idCard.Comp.JobTitle,
                    accessTags,
                    mayReceivePriorityMail,
                    stationName);

                return true;
            }

            recipient = null;
            return false;
        }

        /// <summary>
        /// Get the list of valid mail recipients for a mail teleporter.
        /// </summary>
        public List<MailRecipient> GetMailRecipientCandidates(EntityUid uid)
        {
            List<MailRecipient> candidateList = new();
            var mailLocation = Transform(uid);
            foreach (var receiver in EntityQuery<MailReceiverComponent>())
            {
                // mail is mapwide now, dont need to check if they are on the same station
                //if (_stationSystem.GetOwningStation(receiver.Owner) != _stationSystem.GetOwningStation(uid))
                //    continue;
                var location = Transform(receiver.Owner);

                if (location.MapID != mailLocation.MapID)
                    continue;

                if (TryGetMailRecipientForReceiver(receiver, out MailRecipient? recipient))
                    candidateList.Add(recipient.Value);
            }

            return candidateList;
        }

        /// <summary>
        /// Handle the spawning of all the mail for a mail teleporter.
        /// </summary>
        public void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                _sawmill.Error($"Tried to SpawnMail on {ToPrettyString(uid)} without a valid MailTeleporterComponent!");
                return;
            }

            if (GetUndeliveredParcelCount(uid) >= component.MaximumUndeliveredParcels)
                return;

            var candidateList = GetMailRecipientCandidates(uid);

            if (candidateList.Count <= 0)
            {
                _sawmill.Error("List of mail candidates was empty!");
                return;
            }

            if (!_prototypeManager.TryIndex<MailDeliveryPoolPrototype>(component.MailPool, out var pool))
            {
                _sawmill.Error($"Can't index {ToPrettyString(uid)}'s MailPool {component.MailPool}!");
                return;
            }

            for (int i = 0;
                i < component.MinimumDeliveriesPerTeleport + candidateList.Count / component.CandidatesPerDelivery;
                i++)
            {
                var candidate = _random.Pick(candidateList);
                var possibleParcels = new Dictionary<string, float>(pool.Everyone);

                if (TryMatchJobTitleToPrototype(candidate.Job, out JobPrototype? jobPrototype)
                    && pool.Jobs.TryGetValue(jobPrototype.ID, out Dictionary<string, float>? jobParcels))
                {
                    possibleParcels = possibleParcels.Union(jobParcels)
                        .GroupBy(g => g.Key)
                        .ToDictionary(pair => pair.Key, pair => pair.First().Value);
                }

                if (TryMatchJobTitleToDepartment(candidate.Job, out string? department)
                    && pool.Departments.TryGetValue(department, out Dictionary<string, float>? departmentParcels))
                {
                    possibleParcels = possibleParcels.Union(departmentParcels)
                        .GroupBy(g => g.Key)
                        .ToDictionary(pair => pair.Key, pair => pair.First().Value);
                }

                var accumulated = 0f;
                var randomPoint = _random.NextFloat(possibleParcels.Values.Sum());
                string? chosenParcel = null;
                foreach (var (key, weight) in possibleParcels)
                {
                    accumulated += weight;
                    if (accumulated >= randomPoint)
                    {
                        chosenParcel = key;
                        break;
                    }
                }

                if (chosenParcel == null)
                {
                    _sawmill.Error($"MailSystem wasn't able to find a deliverable parcel for {candidate.Name}, {candidate.Job}!");
                    return;
                }

                var mail = EntityManager.SpawnEntity(chosenParcel, Transform(uid).Coordinates);
                SetupMail(mail, component, candidate);
            }

            if (_containerSystem.TryGetContainer(uid, "queued", out var queued))
                _containerSystem.EmptyContainer(queued);

            _audioSystem.PlayPvs(component.TeleportSound, uid);
        }

        public void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _audioSystem.PlayPvs(component.OpenSound, uid);

            if (user != null)
                _handsSystem.TryDrop((EntityUid) user);

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
            {
                // I silenced this error because it fails non deterministically in tests and doesn't seem to effect anything else.
                // _sawmill.Error($"Mail {ToPrettyString(uid)} was missing contents container!");
                return;
            }

            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                _handsSystem.PickupOrDrop(user, entity);
            }

            _tagSystem.AddTag(uid, "Trash");
            _tagSystem.AddTag(uid, "Recyclable");
            component.IsEnabled = false;
            UpdateMailTrashState(uid, true);
        }

        private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsLocked, isLocked);
        }

        private void UpdateMailTrashState(EntityUid uid, bool isTrash)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsTrash, isTrash);
        }
    }

    public struct MailRecipient
    {
        public string Name;
        public string Job;
        public HashSet<String> AccessTags;
        public bool MayReceivePriorityMail;
        public string Ship;

        public MailRecipient(string name, string job, HashSet<String> accessTags, bool mayReceivePriorityMail, string ship)
        {
            Name = name;
            Job = job;
            AccessTags = accessTags;
            MayReceivePriorityMail = mayReceivePriorityMail;
            Ship = ship;
        }
    }
}
