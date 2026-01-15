using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed class StarlightEntitySystem : EntitySystem
{
    [Robust.Shared.IoC.Dependency] private readonly ILogManager _logManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<EntProtoId, EntityUid> _entities = [];
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("StarlightEntitySystem");

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev) => _entities.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEntity<T>(EntityUid uid, [NotNullWhen(true)] out Entity<T> entity, bool log = true)
        where T : class, IComponent?
    {
        entity = default;
        if (!uid.IsValid() || !TryComp(uid, out MetaDataComponent? metadata))
        {
            _sawmill.Error("Entity {EntityUid} invalid", uid);
            return false;
        }

        if (!TryComp<T>(uid, out var comp1))
        {
            if (log)
                _sawmill.Error(
                    "Entity {EntityName}[{EntityPrototype}]:{EntityUid} does not have component {type}",
                    metadata.EntityName, metadata.EntityPrototype, uid, typeof(T));
            return false;
        }

        entity = (uid, comp1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEntity<T1, T2>(EntityUid uid, [NotNullWhen(true)] out Entity<T1, T2> entity, bool log = true)
        where T1 : class, IComponent?
        where T2 : class, IComponent?
    {
        entity = default;
        if (!uid.IsValid() || !TryComp(uid, out MetaDataComponent? metadata))
        {
            _sawmill.Error("Entity {EntityUid} invalid", uid);
            return false;
        }

        if (!TryComp<T1>(uid, out var comp1))
        {
            if (log)
                _sawmill.Error(
                    "Entity {EntityName}[{EntityPrototype}]:{EntityUid} does not have component {type}",
                    metadata.EntityName, metadata.EntityPrototype, uid, typeof(T1));
            return false;
        }

        if (!TryComp<T2>(uid, out var comp2))
        {
            if (log)
                _sawmill.Error(
                    "Entity {EntityName}[{EntityPrototype}]:{EntityUid} does not have component {type}",
                    metadata.EntityName, metadata.EntityPrototype, uid, typeof(T2));
            return false;
        }

        entity = (uid, comp1, comp2);
        return true;
    }

    public bool TryGetSingleton(EntProtoId surgeryOrStep, out EntityUid uid)
    {
        uid = EntityUid.Invalid;

        if (!_prototypes.HasIndex(surgeryOrStep))
        {
            _sawmill.Error(
                "Prototype '{PrototypeId}' is not registered. Cannot retrieve or spawn a singleton entity.",
                surgeryOrStep);
            return false;
        }

        if (!_entities.TryGetValue(surgeryOrStep, out uid) || TerminatingOrDeleted(uid))
        {
            uid = Spawn(surgeryOrStep, MapCoordinates.Nullspace);
            _entities[surgeryOrStep] = uid;
        }

        return true;
    }
}
