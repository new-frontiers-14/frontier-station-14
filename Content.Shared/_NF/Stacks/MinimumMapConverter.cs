

namespace Content.Shared.Stacks;

public sealed partial class MinimumMapConverter : StackAmountConverter
{
    // A list of numbers to check amount against.
    // Should be sorted.
    [DataField(required: true)]
    public List<int> Requirements;

    // Will return the index of the largest entry in Requirements
    public override void Convert(ref StackAmount amount)
    {
        amount.MaxCount = Math.Min(Requirements.Count, amount.MaxCount);
        int newAmount = -1;
        foreach (var requirement in Requirements)
        {
            if (amount.Amount >= requirement)
                newAmount++;
            else
                break;
        }
        amount.Amount = Math.Max(newAmount, 0);
    }
}
