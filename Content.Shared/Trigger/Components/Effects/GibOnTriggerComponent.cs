using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will gib the entity when triggered.
/// If TargetUser is true the user will be gibbed instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GibOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Should gibbing also delete the owners items?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteItems = false;

    // Frontier fields
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
    // End Frontier
}
