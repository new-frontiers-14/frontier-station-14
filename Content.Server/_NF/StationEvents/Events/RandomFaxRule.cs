using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Fax.Components;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.Paper;

namespace Content.Server.StationEvents.Events;

public sealed class RandomFaxRule : StationEventSystem<RandomFaxRuleComponent>
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    protected override void Added(EntityUid uid, RandomFaxRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (component.PreFaxActions != null)
        {
            foreach (var action in component.PreFaxActions)
            {
                action.Initialize();
            }
        }

        if (component.PerRecipientActions != null)
        {
            foreach (var action in component.PerRecipientActions)
            {
                action.Initialize();
            }
        }
    }

    protected override void Started(EntityUid uid, RandomFaxRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation, HasComp<StationJobsComponent>))
            return;

        if (!TryComp<StationDataComponent>(chosenStation, out var stationData))
            return;

        var grid = StationSystem.GetLargestGrid(stationData);

        if (grid is null)
            return;

        EditableFaxPrintout localPrintout = new()
        {
            Content = Loc.GetString(component.Content),
            Name = Loc.GetString(component.Name),
            Label = component.Label != null ? Loc.GetString(component.Label) : null,
            PrototypeId = component.PrototypeId,
            StampState = component.StampState,
            StampedBy = component.StampedBy ?? new(),
            Locked = component.Locked
        };
        string? localAddress = component.FromAddress;
        if (component.PreFaxActions != null)
        {
            foreach (var action in component.PreFaxActions)
            {
                action.Format(uid, ref localPrintout, ref localAddress);
            }
        }

        var faxQuery = _entMan.EntityQueryEnumerator<FaxMachineComponent>();
        while (faxQuery.MoveNext(out var faxUid, out var faxComp))
        {
            if (_stationSystem.GetOwningStation(faxUid) != chosenStation)
                continue;

            EditableFaxPrintout recipientPrintout = localPrintout;
            string? recipientAddress = localAddress;
            if (component.PerRecipientActions != null)
            {
                foreach (var action in component.PerRecipientActions)
                {
                    action.Format(uid, faxUid, faxComp, ref recipientPrintout, ref recipientAddress);
                }
            }

            FaxPrintout printout = new(
                content: recipientPrintout.Content,
                name: recipientPrintout.Name,
                label: recipientPrintout.Label,
                prototypeId: recipientPrintout.PrototypeId,
                stampState: recipientPrintout.StampState,
                stampedBy: recipientPrintout.StampedBy,
                locked: recipientPrintout.Locked
                );
            _faxSystem.Receive(faxUid, printout, recipientAddress, faxComp);
            break;
        }
    }
}
