using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard.BUI;

[NetSerializable, Serializable]
public sealed class ShipyardConsoleInterfaceState : BoundUserInterfaceState
{
    public int Balance;
    public readonly bool AccessGranted;
    public readonly string? ShipDeedTitle;
    public int ShipSellValue;
    public readonly bool IsTargetIdPresent;
    public readonly byte UiKey;

    public ShipyardConsoleInterfaceState(
        int balance,
        bool accessGranted,
        string? shipDeedTitle,
        int shipSellValue,
        bool isTargetIdPresent,
        byte uiKey)
    {
        Balance = balance;
        AccessGranted = accessGranted;
        ShipDeedTitle = shipDeedTitle;
        ShipSellValue = shipSellValue;
        IsTargetIdPresent = isTargetIdPresent;
        UiKey = uiKey;
    }
}
