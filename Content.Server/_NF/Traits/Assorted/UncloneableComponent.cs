namespace Content.Server._NF.Traits.Assorted;

/// <summary>
/// This is used for the uncloneable trait.
/// </summary>
[RegisterComponent]
public sealed partial class UncloneableComponent : Component
{
    /// <summary>
    /// A field to define if we should display the "Genetic incompatibility" warning on health analysers
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Analyzable = true;

    /// <summary>
    /// The loc string used to provide a reason for being unrevivable
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ReasonMessage = "cloning-console-uncloneable-trait-error";
}
