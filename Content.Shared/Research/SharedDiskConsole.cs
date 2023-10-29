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
    public bool CanPrintRare;
    public int PointCost;
    public int PointCostRare;
    public int ServerPoints;

    public DiskConsoleBoundUserInterfaceState(int serverPoints, int pointCost, int pointCostRare, bool canPrint, bool canPrintRare)
    {
        CanPrint = canPrint;
        CanPrintRare = canPrintRare;
        PointCost = pointCost;
        PointCostRare = pointCostRare;
        ServerPoints = serverPoints;
    }
}

[Serializable, NetSerializable]
public sealed class DiskConsolePrintDiskMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class DiskConsolePrintRareDiskMessage : BoundUserInterfaceMessage
{

}
