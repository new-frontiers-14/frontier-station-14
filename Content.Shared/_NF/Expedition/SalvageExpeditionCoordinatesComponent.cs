namespace Content.Shared._NF.Salvage.Components;

using Robust.Shared.GameStates;

/// <summary>
/// Enables a shuttle to travel to a destination with an item inserted into its console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SalvageExpeditionCoordinatesComponent : Component
{
    /// <summary>
    /// Stores Salvage Expedition difficulty which this disk unlocks
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public string Difficulty = "NFModerate";
    /// <summary>
    /// Stores slot name
    /// </summary>
    public static string DiskSlotName = "disk_slot";
}

[Flags]
public enum DifficultyFlags : byte
{
    NFModerate = 0,

    NFHazardous = 1,

    NFExtreme = 2,
}
