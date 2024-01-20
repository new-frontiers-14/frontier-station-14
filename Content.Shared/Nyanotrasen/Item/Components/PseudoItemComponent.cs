using Content.Shared._NF.Cloning;

namespace Content.Shared.Item.PseudoItem;
/// <summary>
/// For entities that behave like an item under certain conditions,
/// but not under most conditions.
/// </summary>
[RegisterComponent]
public sealed partial class PseudoItemComponent : Component, ITransferredByCloning
{
    [DataField("size")]
    public int Size = 120;

    public bool Active = false;

    [DataField]
    public EntityUid? SleepAction;
}
