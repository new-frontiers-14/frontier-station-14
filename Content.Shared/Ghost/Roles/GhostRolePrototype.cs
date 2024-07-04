using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles;

/// <summary>
///     For selectable ghostrole prototypes in ghostrole spawners.
/// </summary>
[Prototype]
public sealed partial class GhostRolePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the ghostrole.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    /// <summary>
    ///     The description of the ghostrole.
    /// </summary>
    [DataField(required: true)]
    public string Description { get; set; } = default!;

    /// <summary>
    ///     The entity prototype of the ghostrole
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EntityPrototype;

    /// <summary>
    ///     Rules of the ghostrole
    /// </summary>
    [DataField(required: true)]
    public string Rules = default!;

    // Frontier
    /// <summary>
    ///     Whether or not the ghost role requires a player to be whitelisted.
    /// </summary>
    [DataField]
    public bool Whitelisted = false;
    // End Frontier
}
