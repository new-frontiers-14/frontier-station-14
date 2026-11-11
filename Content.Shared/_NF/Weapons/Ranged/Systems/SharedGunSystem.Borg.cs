using System.Diagnostics.CodeAnalysis;
using Content.Shared._NF.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Content.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    private SharedContainerSystem _container = default!;

    private void InitializeBorgGun()
    {
        _container = EntityManager.System<SharedContainerSystem>();

        SubscribeLocalEvent<BorgAmmoProviderComponent, TakeAmmoEvent>(OnBorgTakeAmmo);
        SubscribeLocalEvent<BorgAmmoProviderComponent, GetConnectedContainerEvent>(OnGettingConnectedBorgContainer);
    }

    private void OnBorgTakeAmmo(EntityUid uid, BorgAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (TryGetConnectedBorgContainer(uid, component, out var val))
            RaiseLocalEvent(val.Value, args);
    }

    private void OnGettingConnectedBorgContainer(Entity<BorgAmmoProviderComponent> ent, ref GetConnectedContainerEvent args)
    {
        if (TryGetConnectedBorgContainer(ent, ent.Comp, out var val))
            args.ContainerEntity = val;
    }

    private bool TryGetConnectedBorgContainer(EntityUid uid, BorgAmmoProviderComponent component, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;

        if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            return false;

        var user = container.Owner;
        var providerWhitelist = component.ContainerWhitelist;

        if (!TryComp<ContainerManagerComponent>(user, out var containerManager))
            return false;

        foreach (var currentContainer in containerManager.Containers.Values)
        {
            var itemEnumerator = currentContainer.ContainedEntities.GetEnumerator();

            while (itemEnumerator.MoveNext())
            {
                var item = itemEnumerator.Current;

                if (_whitelistSystem.IsWhitelistFailOrNull(providerWhitelist, item))
                    continue;

                slotEntity = item;
                break;
            }
        }

        return (slotEntity != null);
    }
}
