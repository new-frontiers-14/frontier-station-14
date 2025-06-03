using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server._NF.Shipyard.Systems; // Frontier
using Content.Shared._NF.Shipyard.Components; // Frontier
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using static Content.Shared._NF.Shipyard.Components.ShuttleDeedComponent; // Frontier
using Content.Shared.Access;

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardConsoleComponent, SharedIdCardSystem.WriteToTargetIdMessage>(OnWriteToTargetIdMessage);
        SubscribeLocalEvent<IdCardConsoleComponent, SharedIdCardSystem.WriteToShuttleDeedMessage>(OnWriteToShuttleDeedMessage);

        // one day, maybe bound user interfaces can be shared too.
        SubscribeLocalEvent<IdCardConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
    }

    private void OnWriteToTargetIdMessage(EntityUid uid, IdCardConsoleComponent component, SharedIdCardSystem.WriteToTargetIdMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToTargetId(uid, args.FullName, args.JobTitle, args.AccessList, args.JobPrototype, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void OnWriteToShuttleDeedMessage(EntityUid uid, IdCardConsoleComponent component,
        SharedIdCardSystem.WriteToShuttleDeedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToShuttleDeed(uid, args.ShuttleName, args.ShuttleSuffix, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, IdCardConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        List<ProtoId<AccessLevelPrototype>>? possibleAccess = null;
        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdName = EntityManager.GetComponent<MetaDataComponent>(item).EntityName;
            possibleAccess = _accessReader.FindAccessTags(item).ToList();
        }

        IdCardConsoleBoundUserInterfaceState newState;
        // this could be prettier
        if (component.TargetIdSlot.Item is not { Valid: true } targetId)
        {
            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                false,
                null,
                null,
                false,
                null,
                null,
                possibleAccess,
                string.Empty,
                privilegedIdName,
                string.Empty);
        }
        else
        {
            var targetIdComponent = EntityManager.GetComponent<IdCardComponent>(targetId);
            var targetAccessComponent = EntityManager.GetComponent<AccessComponent>(targetId);

            var jobProto = new ProtoId<JobPrototype>(string.Empty); // Frontier: AccessLevelPrototype<JobPrototype
            if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && keyStorage.Key is {} key
                && _record.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                jobProto = record.JobPrototype;
            }

            string?[]? shuttleNameParts = null;
            var hasShuttle = false;
            if (EntityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var comp))
            {
                shuttleNameParts = new[] { comp.ShuttleName, comp.ShuttleNameSuffix };
                hasShuttle = true;
            }

            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                true,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                hasShuttle, // Frontier
                shuttleNameParts, // Frontier
                targetAccessComponent.Tags.ToList(),
                possibleAccess,
                jobProto,
                privilegedIdName,
                Name(targetId));
        }

        _userInterface.SetUiState(uid, IdCardConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="IdCardConsoleComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetId(EntityUid uid,
        string newFullName,
        string newJobTitle,
        List<ProtoId<AccessLevelPrototype>> newAccessList,
        ProtoId<JobPrototype> newJobProto, // Frontier: AccessLevelPrototype<JobPrototype
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        _idCard.TryChangeFullName(targetId, newFullName, player: player);
        _idCard.TryChangeJobTitle(targetId, newJobTitle, player: player);

        if (_prototype.TryIndex<JobPrototype>(newJobProto, out var job)
            && _prototype.TryIndex(job.Icon, out var jobIcon))
        {
            _idCard.TryChangeJobIcon(targetId, jobIcon, player: player);
            _idCard.TryChangeJobDepartment(targetId, job);
        }

        UpdateStationRecord(uid, targetId, newFullName, newJobTitle, job);

        if (!newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var oldTags = _access.TryGetTags(targetId) ?? new List<ProtoId<AccessLevelPrototype>>();
        oldTags = oldTags.ToList();

        var privilegedId = component.PrivilegedIdSlot.Item;

        if (oldTags.SequenceEqual(newAccessList))
            return;

        // I hate that C# doesn't have an option for this and don't desire to write this out the hard way.
        // var difference = newAccessList.Difference(oldTags);
        var difference = newAccessList.Union(oldTags).Except(newAccessList.Intersect(oldTags)).ToHashSet();
        // NULL SAFETY: PrivilegedIdIsAuthorized checked this earlier.
        var privilegedPerms = _accessReader.FindAccessTags(privilegedId!.Value).ToHashSet();
        if (!difference.IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();
        _access.TrySetTags(targetId, newAccessList);

        /*TODO: ECS SharedIdCardConsoleComponent and then log on card ejection, together with the save.
        This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves AA*/
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(targetId):entity} with the following accesses: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");
    }

    /// <summary>
    /// Called whenever an attempt to change the shuttle deed of the target id is made.
    /// Writes data passed from the ui to the shuttle deed and the grid of shuttle.
    /// </summary>
    private void TryWriteToShuttleDeed(EntityUid uid,
        string newShuttleName,
        string newShuttleSuffix,
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        if (!EntityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var shuttleDeed))
            return;
        else
        {
            if (Deleted(shuttleDeed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId);
                return;
            }
        }

        // Ensure the name is valid and follows the convention
        var name = newShuttleName.Trim();
        // The suffix is ignored as per request
        // var suffix = newShuttleSuffix;
        var suffix = shuttleDeed.ShuttleNameSuffix;

        if (name.Length > MaxNameLength)
            name = name[..MaxNameLength];
        // if (suffix.Length > MaxSuffixLength)
        //     suffix = suffix[..MaxSuffixLength];

        _shipyard.TryRenameShuttle(targetId, shuttleDeed, name, suffix);

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):player} has changed the shuttle name of {ToPrettyString(shuttleDeed.ShuttleUid):entity} to {ShipyardSystem.GetFullName(shuttleDeed)}");
    }

    /// <summary>
    /// Returns true if there is an ID in <see cref="IdCardConsoleComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    /// <remarks>
    /// Other code relies on the fact this returns false if privileged Id is null. Don't break that invariant.
    /// </remarks>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, reader);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, string newFullName, ProtoId<AccessLevelPrototype> newJobTitle, JobPrototype? newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;

        if (newJobProto != null)
        {
            record.JobPrototype = newJobProto.ID;
            record.JobIcon = newJobProto.Icon;
        }

        _record.Synchronize(key);
    }
}
