using Content.Server.Construction;
using Content.Shared._NF.Vehicle.Components;
using Content.Shared._NF.Vehicle.EntitySystems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Movement.Systems;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Vehicle.EntitySystems;

public sealed class VehicleUpgradeSystem : SharedVehicleUpgradeSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleUpgradeComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<VehicleUpgradeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnVehicleStartup(Entity<VehicleUpgradeComponent> ent, ref ComponentStartup args)
    {
        var partContainer = _container.EnsureContainer<Container>(ent, VehicleUpgradeComponent.PartContainerName);
        if (partContainer.ContainedEntities.Count > 0)
            // Already initialized, don't add double the parts.
            return;

        ent.Comp.PartContainer = partContainer;

        var xform = Transform(ent);
        foreach (var (part, amount) in ent.Comp.Requirements)
        {
            var partProto = _prototypeManager.Index(part);
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(partProto.StockPartPrototype, xform.Coordinates);

                if (!_container.Insert(p, partContainer))
                    throw new Exception($"Couldn't insert machine part of type {part} to vehicle with prototype {partProto.StockPartPrototype.ToString() ?? "N/A"}!");
            }
        }
    }

    private void OnRefreshMovementSpeedModifiers(Entity<VehicleUpgradeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.CurrentSpeedModifier);
    }

    public void UpdateParts(Entity<VehicleUpgradeComponent> ent)
    {
        // First, let's find the tier for each relevant machine part type.
        var ratingByPartType = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        foreach (var (partType, count) in ent.Comp.Requirements)
        {
            var totalRating = 0;
            foreach (var x in ent.Comp.PartContainer.ContainedEntities)
            {
                if (!_construction.GetMachinePartState(x, out var machinePart) ||
                    machinePart.Part.PartType != partType)
                    // Weird but okay
                    continue;

                var stackCount = machinePart.Stack?.Count ?? 1;
                totalRating += machinePart.Part.Rating * stackCount;
            }

            var averageRating = (float)totalRating / count;
            ratingByPartType.Add(partType, averageRating);
        }

        // Second, calculate the upgrade multiplier per available upgrade.
        foreach (var (target, upgrade) in ent.Comp.AvailableUpgrades)
        {
            var partRating = ratingByPartType.GetValueOrDefault(upgrade.PartType);
            var multiplier = GetUpgradeMultiplier(partRating, upgrade);

            switch (target)
            {
                case UpgradableVehicleProperty.Speed:
                    ent.Comp.CurrentSpeedModifier = multiplier;
                    break;
                default:
                    throw new Exception($"Upgradable vehicle property not handled: {target}");
            }
        }

        // Lastly, housekeeping to update dependent systems.
        Dirty(ent);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private float GetUpgradeMultiplier(float partRating, VehicleUpgrade upgrade)
    {
        var tier = (int)partRating;
        if (tier >= upgrade.UpgradePerTier.Count - 1)
        {
            // We've reached max tier for this vehicle, no need to interpolate.
            return upgrade.UpgradePerTier[^1];
        }
        // Note: partRating < tier + 1, hence partRating - tier is in [0, 1).
        // If partRating is an integer, partRating - tier = 0 and we use the lower tier (fine).
        return MathHelper.Lerp(upgrade.UpgradePerTier[tier], upgrade.UpgradePerTier[tier + 1], partRating - tier);
    }
}
