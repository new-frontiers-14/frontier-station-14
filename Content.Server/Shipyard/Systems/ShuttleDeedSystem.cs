using Content.Shared.Shipyard.Components;
using Content.Shared.Examine;
using Content.Server.Shipyard.Systems;

namespace Content.Shared.Shipyard;

public sealed partial class ShuttleDeedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShuttleDeedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ShuttleDeedComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!string.IsNullOrEmpty(comp.ShuttleName))
        {
            var fullName = ShipyardSystem.GetFullName(comp);
            args.PushMarkup(Loc.GetString("shuttle-deed-examine-text", ("shipname", fullName)));
        }
    }
}
