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
    public bool CanPrintAllResearch;
    public bool CanImportResearch;
    public int PointCost;
    public int PointCostRare;
    public int ServerPoints;

    public DiskConsoleBoundUserInterfaceState(int serverPoints, int pointCost, int pointCostRare, bool canPrint, bool canPrintRare, bool canPrintAllResearch, bool canImportResearch)
    {
        CanPrint = canPrint;
        CanPrintRare = canPrintRare;
        CanPrintAllResearch = canPrintAllResearch;
        PointCost = pointCost;
        PointCostRare = pointCostRare;
        ServerPoints = serverPoints;
        CanImportResearch = canImportResearch;
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

[Serializable, NetSerializable]
public sealed class DiskConsoleEjectResearchMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class DiskConsoleImportResearchMessage : BoundUserInterfaceMessage
{

}
