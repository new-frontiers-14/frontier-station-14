using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.StationRecords;

public sealed class GeneralStationRecordConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneralStationRecordConsoleWindow? _window = default!;

    public GeneralStationRecordConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneralStationRecordConsoleWindow>();
        _window.OnKeySelected += key =>
            SendMessage(new SelectStationRecord(key));
        _window.OnFiltersChanged += (type, filterValue) =>
            SendMessage(new SetStationRecordFilter(type, filterValue));
        _window.OnJobAdd += OnJobsAdd; // Frontier: job modification buttons
        _window.OnJobSubtract += OnJobsSubtract; // Frontier: job modification buttons
        _window.OnDeleted += id => SendMessage(new DeleteStationRecord(id));
        _window.OnAdvertisementChanged += OnAdvertisementChanged; // Frontier: job modification buttons
    }

    // Frontier: job modification buttons, ship advertisements
    private void OnJobsAdd(ProtoId<JobPrototype> job)
    {
        SendMessage(new AdjustStationJobMsg(job, 1));
    }
    private void OnJobsSubtract(ProtoId<JobPrototype> job)
    {
        SendMessage(new AdjustStationJobMsg(job, -1));
    }
    private void OnAdvertisementChanged(string text)
    {
        SendMessage(new SetStationAdvertisementMsg(text));
    }
    // End Frontier
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GeneralStationRecordConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}
