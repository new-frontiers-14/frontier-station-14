using Content.Shared.Actions;

namespace Content.Shared.SpaceArtillery;

public sealed class SharedSpaceArtillerySystem : EntitySystem
{
}
/// <summary>
/// Raised when someone fires the artillery
/// </summary>
public sealed class FireActionEvent : InstantActionEvent
{
}