using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared._NF.Anomaly;

namespace Content.Server.Anomaly;


/// <summary>
/// This handles expiring links to anomalous vessels.
/// </summary>
public sealed partial class AnomalySystem
{
    [Dependency] private readonly SharedTransformSystem _coords = default!;

    /// <summary> Finish unlocking phase when the time is up. </summary>
    private void UpdateLinkExpiry()
    {
        var query = EntityQueryEnumerator<AnomalyLinkExpiryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;

            CheckLinkExpiry(uid, comp);
        }
    }

    private void CheckLinkExpiry(EntityUid uid, AnomalyLinkExpiryComponent comp)
    {
        // bump the time until next check before anything else happens
        comp.EndTime = _timing.CurTime + comp.CheckFrequency;

        if (TerminatingOrDeleted(uid)
            || !TryComp<AnomalyVesselComponent>(uid, out var vesselComp)
            || vesselComp.Anomaly is not { } anom)
            return;

        if (TryComp(uid, out TransformComponent? xform) && TryComp(anom, out TransformComponent? anomXform))
        {
            // if they're back on the same grid, don't have to worry about it
            if (xform.GridUid == anomXform.GridUid)
            {
                RemComp<AnomalyLinkExpiryComponent>(uid);
                return;
            }

            // if they're within the max distance and are an infection anom, leave the link as is
            if (HasComp<InnerBodyAnomalyComponent>(anom) && _coords.InRange(uid, anom, comp.MaxDistance))
                return;

            vesselComp.Anomaly = null;
            _radiation.SetSourceEnabled(uid, false);
            if (TryComp(anom, out AnomalyComponent? anomComp))
            {
                anomComp.ConnectedVessel = null;
            }
            UpdateVesselAppearance(uid, vesselComp);
            Popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-cleared"), uid);
            RemComp<AnomalyLinkExpiryComponent>(uid);
        }
    }
}
