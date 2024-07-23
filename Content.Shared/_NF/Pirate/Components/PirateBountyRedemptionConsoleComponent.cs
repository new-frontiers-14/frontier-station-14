using Robust.Shared.Audio;

namespace Content.Shared._NF.Pirate.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>
[RegisterComponent]
public sealed partial class PirateBountyRedemptionConsoleComponent : Component
{
    /// <summary>
    /// The sound made when one or more bounties are redeemed
    /// </summary>
    [DataField]
    public SoundSpecifier AcceptSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The sound made when bounty redemption is denied (missing resources)
    /// </summary>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");

    /// <summary>
    /// The last time a bounty redemption was attemped.
    /// </summary>
    [DataField(serverOnly: true)]
    public TimeSpan LastRedeemAttempt = TimeSpan.Zero;
}
