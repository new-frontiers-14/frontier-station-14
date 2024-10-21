using Robust.Shared.Audio;

namespace Content.Server._NF.Medical;

/// <summary>
/// This is used on machines that can be used to redeem medical bounties.
/// </summary>
[RegisterComponent]
public sealed partial class MedicalBountyRedemptionComponent : Component
{
    /// <summary>
    /// The name of the container that holds medical bounties to be redeemed.
    /// </summary>
    [DataField(required: true)]
    public string BodyContainer;

    /// <summary>
    /// The sound that plays when a medical bounty is redeemed successfully.
    /// </summary>
    [DataField]
    public SoundSpecifier RedeemSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The sound that plays when a medical bounty is unsuccessfully redeemed.
    /// </summary>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
}
