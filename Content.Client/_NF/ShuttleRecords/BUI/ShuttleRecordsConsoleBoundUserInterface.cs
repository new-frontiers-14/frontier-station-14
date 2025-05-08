using Content.Client._NF.ShuttleRecords.UI;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared._NF.ShuttleRecords.Components;
using Content.Shared._NF.ShuttleRecords.Events;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface;

namespace Content.Client._NF.ShuttleRecords.BUI;

public sealed class ShuttleRecordsConsoleBoundUserInterface(
    EntityUid owner,
    Enum uiKey
) : BoundUserInterface(owner, uiKey)
{
    private ShuttleRecordsWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            _window = this.CreateWindow<ShuttleRecordsWindow>();
            _window.OnCopyDeed += CopyDeed;
            _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(ShuttleRecordsConsoleComponent.TargetIdCardSlotId));
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ShuttleRecordsConsoleInterfaceState shuttleRecordsConsoleInterfaceState)
            return;

        _window?.UpdateState(shuttleRecordsConsoleInterfaceState);
    }

    private void CopyDeed(ShuttleRecord shuttleRecord)
    {
        if (!EntMan.GetEntity(shuttleRecord.EntityUid).Valid)
            return;

        SendMessage(new CopyDeedMessage(shuttleRecord.EntityUid));
    }

}
