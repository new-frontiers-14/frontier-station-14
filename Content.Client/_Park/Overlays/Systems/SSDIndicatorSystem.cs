// // using Content.Shared._Park.CCVar;
// // using Content.Shared._Park.Overlays.SSDIndicator;
// using Content.Shared.SSDIndicator;
// using Robust.Client.GameObjects;
// using Robust.Shared.Configuration;
// using Robust.Shared.Utility;

// namespace Content.Client._Park.Overlays;

// public sealed class SSDIndicatorSystem : EntitySystem
// {
//     [Dependency] private readonly IEntityManager _entity = default!;
//     [Dependency] private readonly IConfigurationManager _config = default!;


//     public override void Initialize()
//     {
//         base.Initialize();

//         SubscribeLocalEvent<SSDIndicatorComponent, ComponentShutdown>(OnShutdown);
//     }


//     private void OnShutdown(EntityUid uid, SSDIndicatorComponent component, ComponentShutdown args)
//     {
//         if (component.IsSSD)
//         {
//             var sprite = _entity.GetComponent<SpriteComponent>(uid);
//             sprite.LayerSetVisible(SSDIndicatorLayer.SSDIndicator, false);
//         }
//     }

//     public override void Update(float time)
//     {
//         var query = _entity.EntityQueryEnumerator<SSDIndicatorComponent>();

//         while (query.MoveNext(out var uid, out var indicator))
//         {
//             // Update less often
//             if (indicator.Updated)
//             {
//                 indicator.Updated = false;
//                 continue;
//             }
//             indicator.Updated = true;


//             var sprite = _entity.GetComponent<SpriteComponent>(uid);

//             // if (indicator.IsSSD && _config.GetCVar(SimpleStationCCVars.SSDIndicatorEnabled))
//             // {
//             //     // If the layer already exists, just make it visible
//             //     if (sprite.LayerExists(SSDIndicatorLayer.SSDIndicator))
//             //     {
//             //         sprite.LayerSetVisible(SSDIndicatorLayer.SSDIndicator, true);
//             //     }
//             //     // If the layer doesn't exist, create it, ensure it's visible
//             //     else
//             //     {
//             //         var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(indicator.RsiPath, indicator.RsiState));
//             //         sprite.LayerMapSet(SSDIndicatorLayer.SSDIndicator, layer);
//             //         sprite.LayerSetVisible(SSDIndicatorLayer.SSDIndicator, true);
//             //     }
//             // }
//             // else
//             // {
//                 // If the layer exists, hide it, doesn't matter if it doesn't exist
//                 if (sprite.LayerExists(SSDIndicatorLayer.SSDIndicator))
//                     sprite.LayerSetVisible(SSDIndicatorLayer.SSDIndicator, false);
//             // }
//         }
//     }
// }

// public enum SSDIndicatorLayer : byte
// {
//     SSDIndicator,
// }
