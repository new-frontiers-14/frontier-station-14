using Content.Shared._NF.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    private SharedContainerSystem _container = default!;

    private void InitializeBorgGun()
    {
        _container = EntityManager.System<SharedContainerSystem>();


        SubscribeLocalEvent<BorgAmmoProviderComponent, TakeAmmoEvent>(OnBorgTakeAmmo);
    }

    private void OnBorgTakeAmmo(EntityUid uid, BorgAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (!_container.TryGetContainingContainer((uid, null, null), out var container))
        {
            Log.Warning("No Container");
            return;
        }
        var user = container.Owner;
        var providerWhitelist = component.ContainerWhitelist;

        if (!TryComp<ContainerManagerComponent>(user, out var containerManager))
            return;

        var tankId = EntityUid.Invalid;

        foreach (var currentContainer in containerManager.Containers.Values)
        {
            var itemEnumerator = currentContainer.ContainedEntities.GetEnumerator();

            while (itemEnumerator.MoveNext())
            {
                var item = itemEnumerator.Current;

                if (_whitelistSystem.IsWhitelistFailOrNull(providerWhitelist, item))
                    continue;

                tankId = item;
                break;
            }
        }

        if (tankId == EntityUid.Invalid)
            return;

        RaiseLocalEvent(tankId, args);
    }
}
