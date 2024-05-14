using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard.BUI;

[NetSerializable, Serializable]
public sealed class ShipyardConsoleInterfaceState : BoundUserInterfaceState
{
    public ulong Balance;
    public readonly bool AccessGranted;
    public readonly string? ShipDeedTitle;
    public ulong ShipSellValue;
    public readonly bool IsTargetIdPresent;
    public readonly byte UiKey;

    public readonly List<string> ShipyardPrototypes;
    public readonly string ShipyardName;

    public ShipyardConsoleInterfaceState(
        ulong balance,
        bool accessGranted,
        string? shipDeedTitle,
        ulong shipSellValue,
        bool isTargetIdPresent,
        byte uiKey,
        List<string> shipyardPrototypes,
        string shipyardName)
    {
        Balance = balance;
        AccessGranted = accessGranted;
        ShipDeedTitle = shipDeedTitle;
        ShipSellValue = shipSellValue;
        IsTargetIdPresent = isTargetIdPresent;
        UiKey = uiKey;
        ShipyardPrototypes = shipyardPrototypes;
        ShipyardName = shipyardName;
    }
}
