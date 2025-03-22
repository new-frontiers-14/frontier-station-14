using Content.Shared._NF.Bank.Components;

namespace Content.Server._NF.Medical;

[RegisterComponent]
public sealed partial class MedicalBountyBankPaymentComponent : Component
{
    [DataField(required: true)]
    public SectorBankAccount Account;
}
