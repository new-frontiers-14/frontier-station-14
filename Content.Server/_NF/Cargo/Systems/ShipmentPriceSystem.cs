using Content.Server.Cargo.Components;
using Content.Server.Shuttles.Systems;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{

    [Dependency] private readonly DockingSystem _docking = default!;
    private void InitializeShipmentPrice()
    {
        SubscribeLocalEvent<ShipmentPriceComponent, PriceCalculationEvent>(OnShipmentGetPriceEvent);
    }

    private void OnShipmentGetPriceEvent(Entity<ShipmentPriceComponent> entity, ref PriceCalculationEvent ev)
    {
        if (!TryComp(entity, out TransformComponent? xform))
        {
            return; //how do we not have a transform?
        }
        var currentShuttle = xform.GridUid;
        if (currentShuttle != null)
        {
            var shuttleDocks = _docking.GetDocks((EntityUid)currentShuttle); //check we're docked to a destination grid
            foreach (var shuttleDock in shuttleDocks)
            {
                // If the ship we're on is docked with a dock that is on a grid that is tagged, add bonus price and break.
                if (shuttleDock.Comp.DockedWith != null && TryComp<TransformComponent>(shuttleDock.Comp.DockedWith, out var dock) && HasComp<ShipmentRecieveComponent>(dock.GridUid))
                {
                    ev.Price = entity.Comp.BonusPrice;
                    break;
                }
            }
        }
    }
}
