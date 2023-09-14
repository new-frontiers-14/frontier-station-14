﻿using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles the anomaly scanner and it's UI updates.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeScanner()
    {
        SubscribeLocalEvent<AnomalyScannerComponent, BoundUIOpenedEvent>(OnScannerUiOpened);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyScannerComponent, ScannerDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
    }

    private void OnScannerAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            _ui.TryCloseAll(uid, AnomalyScannerUiKey.Key);
        }
    }

    private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(uid, component);
        }
    }

    private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(uid, component);
        }
    }

    private void OnScannerAnomalyHealthChanged(ref AnomalyHealthChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(uid, component);
        }
    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }

    private void OnScannerAfterInteract(EntityUid uid, AnomalyScannerComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;
        if (!HasComp<AnomalyComponent>(target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ScanDoAfterDuration, new ScannerDoAfterEvent(), uid, target: target, used: uid)
        {
            DistanceThreshold = 2f
        });
    }

    private void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        Audio.PlayPvs(component.CompleteSound, uid);
        Popup.PopupEntity(Loc.GetString("anomaly-scanner-component-scan-complete"), uid);
        UpdateScannerWithNewAnomaly(uid, args.Args.Target.Value, component);

        if (TryComp<ActorComponent>(args.Args.User, out var actor)) _ui.TryOpen(uid, AnomalyScannerUiKey.Key, actor.PlayerSession);

        args.Handled = true;
    }

    public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomalyComponent))
            nextPulse = anomalyComponent.NextPulseTime;

        var state = new AnomalyScannerUserInterfaceState(GetScannerMessage(component), nextPulse);
        _ui.TrySetUiState(uid, AnomalyScannerUiKey.Key, state);
    }

    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalyScannerComponent? scannerComp = null, AnomalyComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAnomaly = anomaly;
        UpdateScannerUi(scanner, scannerComp);
    }

    public FormattedMessage GetScannerMessage(AnomalyScannerComponent component)
    {
        var msg = new FormattedMessage();
        if (component.ScannedAnomaly is not { } anomaly || !TryComp<AnomalyComponent>(anomaly, out var anomalyComp))
        {
            msg.AddMarkup(Loc.GetString("anomaly-scanner-no-anomaly"));
            return msg;
        }

        msg.AddMarkup(Loc.GetString("anomaly-scanner-severity-percentage", ("percent", anomalyComp.Severity.ToString("P"))));
        msg.PushNewline();
        string stateLoc;
        if (anomalyComp.Stability < anomalyComp.DecayThreshold)
            stateLoc = Loc.GetString("anomaly-scanner-stability-low");
        else if (anomalyComp.Stability > anomalyComp.GrowthThreshold)
            stateLoc =  Loc.GetString("anomaly-scanner-stability-high");
        else
            stateLoc =  Loc.GetString("anomaly-scanner-stability-medium");
        msg.AddMarkup(stateLoc);
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("anomaly-scanner-point-output", ("point", GetAnomalyPointValue(anomaly, anomalyComp))));
        msg.PushNewline();
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-readout"));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-danger", ("type", GetParticleLocale(anomalyComp.SeverityParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-unstable", ("type", GetParticleLocale(anomalyComp.DestabilizingParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-containment", ("type", GetParticleLocale(anomalyComp.WeakeningParticleType))));

        //The timer at the end here is actually added in the ui itself.
        return msg;
    }
}
