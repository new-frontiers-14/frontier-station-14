using Content.Shared._NF.Radar;
using Content.Shared.Emag.Systems;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedJetpackSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;

    public void NfInitialize()
    {
        SubscribeLocalEvent<JetpackComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<JetpackComponent, GotUnEmaggedEvent>(OnUnEmagged);
    }

    private void OnEmagged(EntityUid uid, JetpackComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        component.RadarBlip = false;
        RemComp<RadarBlipComponent>(uid); // This is needed if you emag mid flight

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
        if (HasComp<ActiveJetpackComponent>(uid))
            SetupRadarBlip(uid);

        args.Handled = true;
    }

    private void SetupRadarBlip(EntityUid uid)
    {
        var blip = EnsureComp<RadarBlipComponent>(uid);
        blip.RadarColor = Color.Cyan;
        blip.Scale = 1f;
        blip.VisibleFromOtherGrids = true;
        blip.RequireNoGrid = true;
    }
}
