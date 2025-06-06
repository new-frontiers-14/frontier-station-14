using Content.Shared._NF.Radar;
using Content.Shared.Emag.Systems;
using Content.Shared.Movement.Components;

namespace Content.Shared._NF.Movement.Systems;

public sealed class SharedJetpackSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JetpackComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<JetpackComponent, GotUnEmaggedEvent>(OnUnEmagged);
    }

    private void OnEmagged(EntityUid uid, JetpackComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (component.RadarBlip)
        {
            component.RadarBlip = false;
            RemComp<RadarBlipComponent>(uid); // This is needed if you emag mid flight
        }

        args.Handled = true;
    }

    private void OnUnEmagged(EntityUid uid, JetpackComponent component, ref GotUnEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (component.RadarBlip)
            return;

        component.RadarBlip = true;

        args.Handled = true;
    }
}
