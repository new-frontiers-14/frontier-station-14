using Content.Shared.StationRecords;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
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

        _window = new();
        _window.OnKeySelected += OnKeySelected;
        _window.OnFiltersChanged += OnFiltersChanged;
        _window.OnJobAdd += OnJobsAdd;
        _window.OnJobSubtract += OnJobsSubtract;
        _window.OnClose += Close;

        _window.OpenCentered();
    }

    private void OnKeySelected((NetEntity, uint)? key)
    {
        SendMessage(new SelectGeneralStationRecord(key));
    }

    private void OnFiltersChanged(
        GeneralStationRecordFilterType type, string filterValue)
    {
        GeneralStationRecordsFilterMsg msg = new(type, filterValue);
        SendMessage(msg);
    }

    private void OnJobsAdd(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
        {
            return;
        }

        AdjustStationJobMsg msg = new(row.Job, 1);
        SendMessage(msg);
    }
    private void OnJobsSubtract(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
        {
            return;
        }
        AdjustStationJobMsg msg = new(row.Job, -1);
        SendMessage(msg);
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GeneralStationRecordConsoleState cast)
        {
            return;
        }

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
