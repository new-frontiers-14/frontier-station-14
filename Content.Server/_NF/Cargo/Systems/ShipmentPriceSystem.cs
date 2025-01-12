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
        if (!TryComp<TransformComponent>(entity, out var xform))
        {
            return; //how do we not have a transform?
        }
        var currentShuttle = xform.GridUid;
        if (currentShuttle != null)
        {
            var shuttleDocks = _docking.GetDocks((EntityUid)currentShuttle); //check we're docked to a destination grid
            foreach (var shuttleDock in shuttleDocks)
            {
                if (shuttleDock.Comp.DockedWith != null)
                {
                    if (TryComp<TransformComponent>(shuttleDock.Comp.DockedWith, out var dock))
                    {
                        if (HasComp<ShipmentRecieveComponent>(dock.GridUid))
                        {
                            ev.Price = entity.Comp.BonusPrice;
                            break;
                        }
                    }

                }
            }

        }

    }

}
