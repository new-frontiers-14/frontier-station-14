using Content.Server._NF.SectorServices.Prototypes;
using Content.Server.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.Map;
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
    private EntityUid _entity = EntityUid.Invalid;

    public void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        _entity = _entityManager.SpawnEntity(null, MapCoordinates.Nullspace);

        foreach (var servicePrototype in _prototypeManager.EnumeratePrototypes<SectorServicePrototype>())
        {
            _entityManager.AddComponents(_entity, servicePrototype.Components, false); // removeExisting false - do not override existing components.
        }
    }

    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (_entity != EntityUid.Invalid)
            QueueDel(_entity);
    }

    // TODO: hide access to the entity directly by requesting its components.
    public EntityUid GetServiceEntity()
    {
        return _entity;
    }
}