using Content.Shared.Damage;
using Content.Shared.Hands;
using Robust.Shared.Containers;

namespace Content.Server.Mail
{
    /// <summary>
    /// A placeholder for another entity, spawned when dropped or placed in someone's hands.
    /// Useful for storing instant effect entities, e.g. smoke, in the mail.
    /// </summary>
    public sealed class DelayedItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DelayedItemComponent, DropAttemptEvent>(OnDropAttempt);
            SubscribeLocalEvent<DelayedItemComponent, GotEquippedHandEvent>(OnHandEquipped);
            SubscribeLocalEvent<DelayedItemComponent, DamageChangedEvent>(OnDamageChanged);
            //SubscribeLocalEvent<DelayedItemComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
            SubscribeLocalEvent<DelayedItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        }

        /// <summary>
        /// MoveEvent handler - item has been dropped or placed on the ground, replace with delayed item.
        /// </summary>
        private void OnRemovedFromContainer(EntityUid uid, DelayedItemComponent component, ContainerModifiedMessage args)
        {
            ReplaceItemInContainer(uid, component, args.Container);
        }

        /// <summary>
        /// HandSelectedEvent handler - item has put into a player's hand, replace with delayed item.
        /// </summary>
        private void OnHandEquipped(EntityUid uid, DelayedItemComponent component, EquippedHandEvent args)
        {
            //ReplaceItem(uid, component);
            DeleteEntity(uid);
        }

        private void OnDropAttempt(EntityUid uid, DelayedItemComponent component, DropAttemptEvent args)
        {
            //ReplaceItem(uid, component);
            DeleteEntity(uid);
        }

        /// <summary>
        /// HandSelectedEvent handler - item has put into a player's hand, replace with delayed item.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, DelayedItemComponent component, DamageChangedEvent args)
        {
            //ReplaceItem(uid, component);
            Spawn(component.Item, Transform(uid).Coordinates);
            EntityManager.DeleteEntity(uid);
        }

        /// <summary>
        /// Replacement mechanism.  Delays spawning a particular item, deletes the delaying component.
        /// </summary>
        private void ReplaceItemInContainer(EntityUid uid, DelayedItemComponent component, BaseContainer container)
        {
            //EntityManager.SpawnInContainerOrDrop(component.Item, container.Owner, container.ID, Transform(uid));
            //EntityManager.Spawn(component.Item, Transform(uid).Coordinates);
            Spawn(component.Item, Transform(uid).Coordinates);
        }

        private void DeleteEntity(EntityUid uid)
        {
            EntityManager.DeleteEntity(uid);
        }
    }
}
