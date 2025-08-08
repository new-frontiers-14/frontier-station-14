using Content.Client.Examine;
using Content.Client._NF.Atmos.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Atmos.Systems;

// Gas recyclers show pipe direction on examine, arrow sprite reused from TEG functionality
public sealed class NFGasRecyclerSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string ArrowPrototype = "TegCirculatorArrow";

    public override void Initialize()
    {
        SubscribeLocalEvent<NFGasRecyclerVisualsComponent, ClientExaminedEvent>(RecyclerExamined);
    }

    private void RecyclerExamined(EntityUid uid, NFGasRecyclerVisualsComponent component, ClientExaminedEvent args)
    {
        Spawn(ArrowPrototype, new EntityCoordinates(uid, 0, 0));
    }
}
