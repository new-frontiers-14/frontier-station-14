using Content.Shared.Dispenser;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Dispenser
{
    public sealed class DispenserSystem : SharedDispenserSystem
    {
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DispenserComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<DispenserComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnActivateInWorld(EntityUid uid, DispenserComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
            {
                return;
            }

            if (!string.IsNullOrEmpty(component.DefaultItem))
            {
                args.Handled = true;
                TryDispenseItem(uid, component, component.DefaultItem);
            }
            else
            {
                _audioSystem.PlayPvs(component.DenySound, uid);
            }
        }

        private void OnInteractUsing(EntityUid uid, DispenserComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
            {
                return;
            }

            if (TryPrototype(args.Used, out var prototype)
                && TryGetDispenseItem(component, prototype.ID, out string itemId))
            {
                args.Handled = true;
                QueueDel(args.Used);
                TryDispenseItem(uid, component, itemId);
            }
            else
            {
                _audioSystem.PlayPvs(component.DenySound, uid);
            }
        }

        public bool TryGetDispenseItem(DispenserComponent component, string itemId, out string dispenseItemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                dispenseItemId = string.Empty;
                return false;
            }

            foreach (var kvp in component.Inventory)
            {
                if (kvp.Key == itemId)
                {
                    dispenseItemId = kvp.Value;
                    return !string.IsNullOrEmpty(dispenseItemId);
                }
            }

            dispenseItemId = string.Empty;
            return false;
        }

        public void TryDispenseItem(EntityUid uid, DispenserComponent component, string itemId)
        {
            component.Dispensing = true;
            component.DispensingItemId = itemId;
            component.DispenseTimer = 0f;

            _audioSystem.PlayPvs(component.DispenseSound, uid);
        }

        public void Dispense(EntityUid uid, DispenserComponent component, string itemId)
        {
            var entity = Spawn(itemId, Transform(uid).Coordinates);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<DispenserComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                if (component.Dispensing)
                {
                    component.DispenseTimer += frameTime;
                    if (component.DispenseTimer >= component.DispenseTime)
                    {
                        component.DispenseTimer = 0f;
                        component.Dispensing = false;

                        Dispense(uid, component, component.DispensingItemId);
                    }
                }
            }
        }
    }
}
