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
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? NextDrop;

    /// <summary>
    ///     A non-nullable proxy to overwrite NextDrop
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDropVV
    {
        get { return NextDrop ?? TimeSpan.Zero; }
        set { NextDrop = value; }
    }

    /// <summary>
    ///     Minimum wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField]
    //Use 10 seconds for testing
    public int MinimumCoolDown = 900; // 900 / 60 = 15 minutes

    /// <summary>
    ///     Max wait time in seconds to wait for the next dead drop.
    /// </summary>
    [DataField]
    //Use 15 seconds for testing
    public int MaximumCoolDown = 5400; // 5400 / 60 = 90 minutes

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
    ///     The paper prototype to spawn.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HintPaper = "PaperCargoInvoice";

    /// <summary>
    ///     Whether or not a drop pod has been called for this dead drop.
    /// </summary>
    [DataField]
    public bool DeadDropCalled = false;

    /// <summary>
    ///     Location of the grid to spawn in as the dead drop.
    /// </summary>
    [DataField]
    public string DropGrid = "/Maps/_NF/DeadDrop/deaddrop.yml";

    /// <summary>
    ///     The color of your grid. the name should be set by the mapper when mapping.
    /// </summary>
    [DataField]
    public Color Color = new(225, 15, 155);
}
