using Content.Server._DV.Mail.Components;
using Content.Shared.Damage;
using Robust.Shared.Containers;

namespace Content.Server._DV.Mail.EntitySystems
{
    /// <summary>
    /// A placeholder for another entity, spawned when an entity is taken out of a container, with the placeholder deleted shortly after.
    /// Useful for storing instant effect entities, e.g. smoke, in the mail.
    /// Note: for items with ghost roles, ensure that the item is not damageable.
    /// </summary>
    public sealed class DelayedItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DelayedItemComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<DelayedItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        }

        /// <summary>
        /// EntGotRemovedFromContainerMessage handler - spawn the intended entity after removed from a container and delete the.
        /// </summary>
        private void OnRemovedFromContainer(EntityUid uid, DelayedItemComponent component, EntGotRemovedFromContainerMessage args)
        {
            SpawnAtPosition(component.Item, Transform(uid).Coordinates);
            QueueDel(uid);
        }

        /// <summary>
        /// OnDamageChanged handler - item has taken damage (e.g. inside the envelope), spawn the intended entity in the same container as the placeholder and delete the placeholder.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, DelayedItemComponent component, DamageChangedEvent args)
        {
            SpawnAtPosition(component.Item, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }
}
