namespace Content.Server.Explosion.Components;

/// <summary>
/// Gibs on trigger, self explanatory.
/// Also in case of an implant using this, gibs the implant user instead.
/// </summary>
[RegisterComponent]
public sealed partial class GibOnTriggerComponent : Component
{
    /// <summary>
    /// Should gibbing also delete the owners items?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deleteItems")]
    public bool DeleteItems = false;

    /// <summary>
    /// Frontier - Should gibbing also delete the owners organs?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool DeleteOrgans = false;

    /// <summary>
    /// Frontier - Do we want to go through with the gibbing?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Gib = true;

    /// <summary>
    /// Frontier - Should the argument entity be used?
    /// False: default existing behaviour, uses transform parent
    /// True: uses entity passed in
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool UseArgumentEntity = false;
}
