using Robust.Shared.Player;

namespace Content.Shared._NF.Bank.Events;
public sealed class BalanceChangedEvent : EntityEventArgs
{
    public readonly int Amount;
    public readonly ICommonSession Player;

    public BalanceChangedEvent(int amount, ICommonSession player)
    {
        Amount = amount;
        Player = player;
    }
}
