namespace Content.Server._NF.Traits.Assorted;

/// <summary>
/// This is used for the unclonable trait.
/// </summary>
[RegisterComponent]
public sealed partial class UnclonableComponent : Component
{
    /// <summary>
    /// A field to define if we should display a warning on health analyzers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Analyzable = true;

    /// <summary>
    /// The loc string used to provide a reason for being unclonable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ReasonMessage = "cloning-console-uncloneable-trait-error";
}
