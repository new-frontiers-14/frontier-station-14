using Robust.Shared.GameStates;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Implant to get BibleUser status (to pray, summon familiars, bless with bibles)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BibleUserImplantComponent : Component
{
    /// <summary>
    /// Set to true if the implant caused the user to be pacified.
    /// </summary>
    public bool PacifiedUser = false;
}
