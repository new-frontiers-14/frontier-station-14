namespace Content.Server._NF.Medical;

/// <summary>
/// This is used on machines that can be used to redeem medical bounties.
/// </summary>
[RegisterComponent]
public sealed partial class MedicalBountyRedeemerComponent : Component
{
    /// <summary>
    /// The name of the container that holds medical bounties to be redeemed.
    /// </summary>
    public string BodyContainer;
}
