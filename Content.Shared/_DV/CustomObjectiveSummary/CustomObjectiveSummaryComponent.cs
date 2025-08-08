using Robust.Shared.GameStates;

namespace Content.Shared._DV.CustomObjectiveSummary;

/// <summary>
///     Put on a players mind if the wrote a custom summary for their objectives.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CustomObjectiveSummaryComponent : Component
{
    /// <summary>
    ///     What the player wrote as their summary!
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ObjectiveSummary = "";
}
