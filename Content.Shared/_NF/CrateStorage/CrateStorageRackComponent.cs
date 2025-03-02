using Robust.Shared.GameStates;

namespace Content.Shared._NF.CrateStorage;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrateStorageRackComponent : Component
{
    /// <summary>
    /// The amount of crates stored in this rack, used for visual state.
    /// </summary>
    [DataField]
    public int StoredCrates;
};
