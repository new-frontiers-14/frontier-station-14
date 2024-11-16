using Content.Shared._NF.Medical.Prototypes;

namespace Content.Server._NF.Medical.Components;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class MedicalBountyComponent : Component
{
    /// <summary>
    /// The bounty to use/used for damage generation.
    /// If null, a medical bounty type will be selected at random.
    /// </summary>
    [DataField(serverOnly: true)]
    public MedicalBountyPrototype? Bounty = null;

    /// <summary>
    /// Maximum bounty value for this entity in spesos.
    /// Cached from bounty params on generation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int MaxBountyValue;

    /// <summary>
    /// Ensures damage is only applied once, set to true on startup.
    /// </summary>
    public bool BountyInitialized;
}
