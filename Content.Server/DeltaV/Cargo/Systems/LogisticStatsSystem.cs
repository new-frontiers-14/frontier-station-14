using Content.Server.DeltaV.Cargo.Components;
using Content.Shared.Cargo;
using JetBrains.Annotations;

namespace Content.Server.DeltaV.Cargo.Systems;

public sealed partial class LogisticStatsSystem : SharedCargoSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    [PublicAPI]
    public void AddOpenedMailEarnings(SectorLogisticStatsComponent component, int earnedMoney)
    {
        component.Metrics = component.Metrics with
        {
            Earnings = component.Metrics.Earnings + earnedMoney,
            OpenedCount = component.Metrics.OpenedCount + 1
        };
        UpdateLogisticsStats();
    }

    [PublicAPI]
    public void AddExpiredMailLosses(SectorLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            ExpiredLosses = component.Metrics.ExpiredLosses + lostMoney,
            ExpiredCount = component.Metrics.ExpiredCount + 1
        };
        UpdateLogisticsStats();
    }

    [PublicAPI]
    public void AddDamagedMailLosses(SectorLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            DamagedLosses = component.Metrics.DamagedLosses + lostMoney,
            DamagedCount = component.Metrics.DamagedCount + 1
        };
        UpdateLogisticsStats();
    }

    [PublicAPI]
    public void AddTamperedMailLosses(SectorLogisticStatsComponent component, int lostMoney)
    {
        component.Metrics = component.Metrics with
        {
            TamperedLosses = component.Metrics.TamperedLosses + lostMoney,
            TamperedCount = component.Metrics.TamperedCount + 1
        };
        UpdateLogisticsStats();
    }

    private void UpdateLogisticsStats() => RaiseLocalEvent(new LogisticStatsUpdatedEvent());
}

public sealed class LogisticStatsUpdatedEvent : EntityEventArgs
{
    public LogisticStatsUpdatedEvent()
    {
    }
}
