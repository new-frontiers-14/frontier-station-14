using Content.Server.Power.NodeGroups;
using Content.Server.Power.Components;

namespace Content.Server._NF.Power.Components;

/// <summary>
///     Attempts to link with a nearby <see cref="ApcPowerProviderComponent"/>s
///     so that it can receive power from a <see cref="IApcNet"/>.
///     If no power provider is found, the system will pull power from installed cells instead.
/// </summary>
[RegisterComponent]
public sealed partial class MixedPowerReceiverComponent : Component
{
    /// <summary>
     /// How many watts (from the battery) does the device need?
     /// </summary>
    [DataField]
    public float Wattage = 5f;
}

