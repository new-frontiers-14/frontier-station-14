using Content.Shared._NF.SectorServices.Prototypes;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;


namespace Content.Server._NF.SectorServices;

/// <summary>
/// System that manages sector-wide services.
/// Allows service components to be registered and unregistered on a singular entity
/// </summary>
[PublicAPI]
public sealed class SectorServiceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    private EntityUid _entity = EntityUid.Invalid; // The station entity that's storing our services.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationSectorServiceHostComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StationSectorServiceHostComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnComponentInit(EntityUid uid, StationSectorServiceHostComponent component, ComponentInit args)
    {
        Log.Debug($"OnComponentStartup! Entity: {uid} internal: {_entity}");
        if (_entity == EntityUid.Invalid)
        {
            _entity = Spawn();
            component.SectorUid = _entity;

            foreach (var servicePrototype in _prototypeManager.EnumeratePrototypes<SectorServicePrototype>())
            {
                Log.Debug($"Adding components for service {servicePrototype.ID}");
                _entityManager.AddComponents(_entity, servicePrototype.Components, false); // removeExisting false - do not override existing components.
            }
        }
    }

    private void OnComponentRemove(EntityUid uid, StationSectorServiceHostComponent component, ComponentRemove args)
    {
        Log.Debug($"ComponentRemove called! Entity: {_entity}");
        DeleteServiceEntity();
    }

    public void OnCleanup(RoundRestartCleanupEvent _)
    {
        Log.Debug($"RoundRestartCleanup called! Entity: {_entity}");
        DeleteServiceEntity();
    }

    private void DeleteServiceEntity()
    {
        if (EntityManager.EntityExists(_entity) && !Terminating(_entity))
        {
            QueueDel(_entity);
        }
        _entity = EntityUid.Invalid;
    }

    public EntityUid GetServiceEntity()
    {
        return _entity;
    }

    // Component access (mirroring EntityManager without entity ID)
    // WIP
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : IComponent
    // {
    //     return _entityManager.TryGetComponent(_entity, out component);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
    // {
    //     return _entityManager.TryGetComponent(_entity, type, out component);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool TryGetComponent(CompIdx type, [NotNullWhen(true)] out IComponent? component)
    // {
    //     return _entityManager.TryGetComponent(_entity, type, out component);
    // }

    // /// <inheritdoc />
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, Type type,
    //     [NotNullWhen(true)] out IComponent? component)
    // {
    //     return _entityManager.TryGetComponent(_entity, type, out component);
    // }

    // /// <inheritdoc />
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool TryGetComponent(ushort netId, [MaybeNullWhen(false)] out IComponent component, MetaDataComponent? meta = null)
    // {
    //     return _entityManager.TryGetComponent(_entity, netId, out component, meta);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // [Pure]
    // public bool TryComp<T>([NotNullWhen(true)] out T? component) where T : IComponent
    //     => TryGetComponent(out component);

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // [Pure]
    // public T Comp<T>() where T : IComponent
    // {
    //     return GetComponent<T>();
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public T GetComponent<T>() where T : IComponent
    // {
    //     return _entityManager.GetComponent<T>(_entity);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public IComponent GetComponent(CompIdx type)
    // {
    //     return _entityManager.GetComponent(_entity, type);
    // }

    // /// <inheritdoc />
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public IComponent GetComponent(Type type)
    // {
    //     return _entityManager.GetComponent(_entity, type);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // [Pure]
    // public bool HasComponent(EntityUid? uid)
    // {
    //     return uid != null && HasComponent(uid.Value);
    // }
}
