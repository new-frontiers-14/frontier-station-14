using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class AppraisalUiState : BoundUserInterfaceState
{
    /// <summary>
    /// The list of appraised items
    /// </summary>
    public List<AppraisedItem> AppraisedItems;

    public AppraisalUiState(List<AppraisedItem> appraisedItems)
    {
        AppraisedItems = appraisedItems;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class AppraisedItem
{
    public readonly string Name;
    public readonly string AppraisedPrice;

    public AppraisedItem(string name, string appraisedPrice)
    {
        Name = name;
        AppraisedPrice = appraisedPrice;
    }
}
