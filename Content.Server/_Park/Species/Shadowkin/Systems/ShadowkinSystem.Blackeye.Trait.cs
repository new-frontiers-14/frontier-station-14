using Content.Server._Park.Species.Shadowkin.Components;
using Content.Shared._Park.Species.Shadowkin.Events;

namespace Content.Server._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinBlackeyeTraitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinBlackeyeTraitComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, ShadowkinBlackeyeTraitComponent _, ComponentStartup args)
    {
        RaiseLocalEvent(uid, new ShadowkinBlackeyeEvent(uid, false));
        RaiseNetworkEvent(new ShadowkinBlackeyeEvent(uid, false));
    }
}
