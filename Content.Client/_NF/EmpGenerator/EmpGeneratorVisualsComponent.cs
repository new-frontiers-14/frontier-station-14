using Content.Shared.Power;

namespace Content.Shared._NF.EmpGenerator;

[RegisterComponent]
public sealed partial class EmpGeneratorVisualsComponent : Component
{
    /// <summary>
    /// A map of the sprites used by the mobile emp given its status.
    /// </summary>
    [DataField]
    public Dictionary<PowerChargeStatus, string> SpriteMap = new();

    /// <summary>
    /// A list of sprites to draw by maximum charge.
    /// Expected to be in increasing order of MaxCharge.
    /// </summary>
    [DataField]
    public List<EmpCoreSpriteThreshold> Thresholds = new();
}

[DataDefinition]
public partial record struct EmpCoreSpriteThreshold
{
    [DataField]
    public float MaxCharge;
    [DataField]
    public string? State;
    [DataField]
    public bool Visible;
}
