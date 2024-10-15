using System.Linq;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday;

public sealed class HalloweenSystem : EntitySystem
{
    [ValidatePrototypeId<HolidayPrototype>]
    private const string HolidayId = "Halloween";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DeepFryerId = "KitchenDeepFryer";
    [ValidatePrototypeId<EntityPrototype>]
    private const string DeepFryerPOIId = "KitchenDeepFryerPOI";

    [ValidatePrototypeId<EntityPrototype>]
    private const string CauldronId = "KitchenDeepFryerCauldron";
    [ValidatePrototypeId<EntityPrototype>]
    private const string CauldronPOIId = "KitchenDeepFryerCauldronPOI";

    [Dependency] private readonly HolidaySystem _holiday = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEntityReadEvent>(OnBeforeRead);
    }

    private void OnBeforeRead(BeforeEntityReadEvent ev)
    {
        if (_holiday.IsCurrentlyHoliday(HolidayId))
        {
            // Replace all deep fryers with cauldrons, even on POIs
            ev.RenamedPrototypes.TryAdd(DeepFryerId, CauldronId);
            ev.RenamedPrototypes.TryAdd(DeepFryerPOIId, CauldronPOIId);
        }
    }
}
