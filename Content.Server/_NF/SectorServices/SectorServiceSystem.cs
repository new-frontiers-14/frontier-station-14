using Content.Shared._NF.SectorServices.Prototypes;
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
    [Robust.Shared.IoC.Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IEntityManager _entityManager = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    private EntityUid _entity = EntityUid.Invalid; // The station entity that's storing our services.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationSectorServiceHostComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<StationSectorServiceHostComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentStartup(EntityUid uid, StationSectorServiceHostComponent component, ComponentStartup args)
    {
        Log.Debug($"OnComponentStartup! Entity: {uid} internal: {_entity}");
        if (_entity == EntityUid.Invalid)
        {
            _entity = uid;

            foreach (var servicePrototype in _prototypeManager.EnumeratePrototypes<SectorServicePrototype>())
            {
                Log.Debug($"Adding components for service {servicePrototype.ID}");
                _entityManager.AddComponents(_entity, servicePrototype.Components, false); // removeExisting false - do not override existing components.
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, StationSectorServiceHostComponent component, ComponentShutdown args)
    {
        Log.Debug($"OnComponentShutdown! Entity: {_entity}");
        if (_entity != EntityUid.Invalid)
        {
            foreach (var servicePrototype in _prototypeManager.EnumeratePrototypes<SectorServicePrototype>())
            {
                Log.Debug($"Removing component for service {servicePrototype.ID}");
                _entityManager.RemoveComponents(_entity, servicePrototype.Components);
            }
            _entity = EntityUid.Invalid;
        }
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