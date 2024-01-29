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
    [DataField("nextDrop")]
    public TimeSpan? NextDrop;

    /// <summary>
    ///     Minimum wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField("minimumCoolDown")]
    public int MinimumCoolDown = 900; // 900 / 60 = 15 minutes

    /// <summary>
    ///     Max wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField("maximumCoolDown")]
    public int MaximumCoolDown = 5400; // 5400 / 60 = 90 minutes

    /// <summary>
    ///     Minimum distance to spawn the drop.
    /// </summary>
    [DataField("minimumDistance")]
    public int MinimumDistance = 6500;

    /// <summary>
    ///     Max distance to spawn the drop.
    /// </summary>
    [DataField("maximumDistance")]
    public int MaximumDistance = 9900;

    /// <summary>
    ///     The paper prototype to spawn.
    /// </summary>
    [DataField("hintPaper", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HintPaper = "PaperCargoInvoice";

    /// <summary>
    ///     Location of the grid to spawn in as the dead drop.
    /// </summary>
    [DataField("dropGrid")]
    public string DropGrid = "/Maps/deaddrop.yml";

    /// <summary>
    ///     The color of your grid. the name should be set by the mapper when mapping.
    /// </summary>
    [DataField("color")]
    public Color Color = new Color(225, 15, 155);
}
