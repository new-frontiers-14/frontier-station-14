using Robust.Shared.GameStates;

namespace Content.Shared._NF.Chemistry.Components;

/// <summary>
///     Gives click behavior for changing injector reagent whitelist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReagentWhitelistChangeComponent : Component
{
    /// <summary>
    ///     The type of reagents allowed to be selected to change the reagent whitelist
    /// </summary>
    [DataField("allowedReagentGroup")]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> AllowedReagentGroups = new();
}
