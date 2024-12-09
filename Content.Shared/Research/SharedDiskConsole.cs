using Robust.Shared.Serialization;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum DiskConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DiskConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool CanPrint;
    public bool CanPrintRare; // Frontier
    public int PointCost;
    public int PointCostRare; // Frontier
    public int ServerPoints;

    public DiskConsoleBoundUserInterfaceState(int serverPoints, int pointCost, int pointCostRare, bool canPrint, bool canPrintRare) // Frontier: add pointCostRare, canPrintRare
    {
        CanPrint = canPrint;
        CanPrintRare = canPrintRare; // Frontier
        PointCost = pointCost;
        PointCostRare = pointCostRare; // Frontier
        ServerPoints = serverPoints;
    }
}

[Serializable, NetSerializable]
public sealed class DiskConsolePrintDiskMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable] // Frontier
public sealed class DiskConsolePrintRareDiskMessage : BoundUserInterfaceMessage
{

}
