using Robust.Shared.GameStates;

namespace Content.Server._NF.Worldgen.Components.Debris;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpaceDebrisComponent : Component
{
    /// <summary>
    /// TODO: Add this so we can track all the debris active entities.
    /// </summary>
    [DataField]
    public List<EntityUid>? ActiveEntities;
}
