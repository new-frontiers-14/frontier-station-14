using Robust.Shared.Player;

namespace Content.Shared._NF.Bank.Events;
public sealed record BalanceChangedEvent(ICommonSession Session, int Amount);
