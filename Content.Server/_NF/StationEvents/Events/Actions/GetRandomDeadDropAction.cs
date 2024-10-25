using Content.Server._NF.Smuggling;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// An action that gets a set number of dead drops from a 
/// </summary> 
[DataDefinition]
public sealed partial class GetRandomDeadDropAction : IPreFaxAction
{
    private IEntityManager _entityManager = default!;
    private DeadDropSystem _deadDrop = default!;

    public void Initialize()
    {
        _entityManager = IoCManager.Resolve<IEntityManager>();
        _deadDrop = _entityManager.EntitySysManager.GetEntitySystem<DeadDropSystem>();
    }

    public void Format(EntityUid station, ref EditableFaxPrintout printout, ref string? fromAddress)
    {
        printout.Content = _deadDrop.GenerateRandomHint();
    }
}
