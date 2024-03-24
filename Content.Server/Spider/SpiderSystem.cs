using System.Linq;
using Content.Server.Popups;
using Content.Shared.Spider;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Spider;

public sealed class SpiderSystem : SharedSpiderSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderComponent, SpiderWebActionEvent>(OnSpawnNet);
    }

    private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<HungerComponent>(uid, out var hungerComp)
        && _hungerSystem.IsHungerBelowState(uid, HungerThreshold.Okay, hungerComp.CurrentHunger - 5, hungerComp))
        {
            _popup.PopupEntity(Loc.GetString("sericulture-failure-hunger"), args.Performer, args.Performer);
            return;
        }

        var transform = Transform(uid);

        if (transform.GridUid == null)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-nogrid"), args.Performer, args.Performer);
            return;
        }

        var coords = transform.Coordinates;

        // TODO generic way to get certain coordinates

        var result = false;
        // Spawn web in center
        if (!IsTileBlockedByWeb(coords))
        {
            Spawn(component.WebPrototype, coords);
            result = true;
        }

        if (result)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-success"), args.Performer, args.Performer);
            _hungerSystem.ModifyHunger(uid, -5);
            args.Handled = true;
        }
        else
            _popup.PopupEntity(Loc.GetString("spider-web-action-fail"), args.Performer, args.Performer);
    }

    private bool IsTileBlockedByWeb(EntityCoordinates coords)
    {
        foreach (var entity in coords.GetEntitiesInTile())
        {
            if (HasComp<SpiderWebObjectComponent>(entity))
                return true;
        }
        return false;
    }
}

