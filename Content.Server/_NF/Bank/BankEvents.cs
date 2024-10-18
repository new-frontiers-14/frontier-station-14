namespace Content.Server.Bank;

public sealed class BankAccountAddedEvent : EntityEventArgs
{
    public EntityUid Entity;
    public BankAccountAddedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}

public sealed class BankAccountRemovedEvent : EntityEventArgs
{
    public EntityUid Entity;
    public BankAccountRemovedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
