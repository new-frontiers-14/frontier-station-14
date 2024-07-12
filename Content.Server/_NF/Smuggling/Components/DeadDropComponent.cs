using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Store all bounty contracts information.
/// </summary>
[RegisterComponent]
[Access(typeof(DeadDropSystem))]
public sealed partial class DeadDropComponent : Component
{
    /// <summary>
    ///     When the next drop will occur. Used internally.
    /// </summary>
    [DataField]
    public TimeSpan? NextDrop;

    /// <summary>
    ///     Minimum wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField]
    public int MinimumCoolDown = 600; // 600 / 60 = 10 minutes

    /// <summary>
    ///     Max wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField]
    public int MaximumCoolDown = 3000; // 3000 / 60 = 50 minutes

    /// <summary>
    ///     Minimum distance to spawn the drop.
    /// </summary>
    [DataField]
    public int MinimumDistance = 4500;

    /// <summary>
    ///     Max distance to spawn the drop.
    /// </summary>
    [DataField]
    public int MaximumDistance = 6500;

    /// <summary>
    ///     Boolean which confirms whether a poster is activated
    /// </summary>
    [DataField]
    public bool DeadDropActivated = false;

    /// <summary>
    ///     The paper prototype to spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HintPaper = "PaperCargoInvoice";

    /// <summary>
    ///     Location of the grid to spawn in as the dead drop.
    /// </summary>
    [DataField]
    public string DropGrid = "/Maps/deaddrop.yml";

    /// <summary>
    ///     The color of your grid. the name should be set by the mapper when mapping.
    /// </summary>
    [DataField]
    public Color Color = new(225, 15, 155);
}
