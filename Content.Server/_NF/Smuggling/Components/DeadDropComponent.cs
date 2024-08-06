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
    //Use 10 seconds for testing
    public int MinimumCoolDown = 300; // 300 / 60 = 5 minutes

    /// <summary>
    ///     Max wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField]
    //Use 15 seconds for testing
    public int MaximumCoolDown = 600; // 600 / 60 = 10 minutes

    /// <summary>
    ///     Wait time for NSFD to get coordinates of the drop pod location.
    /// </summary>
    [DataField]
    //Use 15 seconds for testing
    public int RadioCoolDown = 900; // 900 / 60 = 15 minutes

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
    ///     Max amount of dead drop posters active at a time
    /// </summary>
    [DataField]
    public int MaxPosters = 2;

    /// <summary>
    ///     Boolean which confirms whether a poster can spawn a dead drop and has the verb to do so
    /// </summary>
    [DataField]
    public bool DeadDropActivated = false;

    /// <summary>
    ///     The paper prototype to spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HintPaper = "PaperCargoInvoice";

    /// <summary>
    ///     Boolean which determines if the dead drop has ever been spawned by this component
    /// </summary>
    [DataField]
    public bool DeadDropCalled = false;

    /// <summary>
    ///     Boolean which determines if the dead drop poster has ever been scanned
    /// </summary>
    [DataField]
    public bool PosterScanned = false;

    /// <summary>
    ///     Location of the grid to spawn in as the dead drop
    /// </summary>
    [DataField]
    public string DropGrid = "/Maps/deaddrop.yml";

    /// <summary>
    ///     The color of your grid. the name should be set by the mapper when mapping.
    /// </summary>
    [DataField]
    public Color Color = new(225, 15, 155);
}
