using Content.Shared.Cargo.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared._NF.Cargo;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed class SharedPricingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    private double GetSolutionPrice(Entity<SolutionContainerManagerComponent> entity)
    {
        if (Comp<MetaDataComponent>(entity).EntityLifeStage < EntityLifeStage.MapInitialized)
            return GetSolutionPrice(entity.Comp);

        var price = 0.0;

        foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((entity.Owner, entity.Comp)))
        {
            var solution = soln.Comp.Solution;
            foreach (var (reagent, quantity) in solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var reagentProto))
                    continue;

                // TODO check ReagentData for price information?
                price += (float) quantity * reagentProto.PricePerUnit;
            }
        }

        return price;
    }

    private double GetSolutionPrice(SolutionContainerManagerComponent component)
    {
        var price = 0.0;

        foreach (var (_, prototype) in _solutionContainerSystem.EnumerateSolutions(component))
        {
            foreach (var (reagent, quantity) in prototype.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Prototype, out var reagentProto))
                    continue;

                // TODO check ReagentData for price information?
                price += (float) quantity * reagentProto.PricePerUnit;
            }
        }

        return price;
    }

    private double GetMaterialPrice(PhysicalCompositionComponent component)
    {
        double price = 0;
        foreach (var (id, quantity) in component.MaterialComposition)
        {
            price += _prototypeManager.Index<MaterialPrototype>(id).Price * quantity;
        }
        return price;
    }

    /// <summary>
    /// Get a rough price for an entityprototype. Does not consider contained entities.
    /// </summary>
    public double GetEstimatedPrice(EntityPrototype prototype)
    {
        double price = 0;
        price += GetMaterialsPrice(prototype);
        price += GetSolutionsPrice(prototype);
        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(prototype);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(prototype);
        }

        // TODO: Proper container support.

        return price;
    }

    /// <summary>
    /// Add a hardcoded price for an item to set how much it will cost to buy it from a vending machine, while allowing staticPrice to set its sell price.
    /// </summary>
    public double GetEstimatedVendPrice(EntityPrototype prototype)
    {
        // TODO: Proper container support.

        return GetVendPrice(prototype);
    }

    /// <summary>
    /// Appraises an entity, returning it's price.
    /// </summary>
    /// <param name="uid">The entity to appraise.</param>
    /// <returns>The price of the entity.</returns>
    /// <remarks>
    /// This fires off an event to calculate the price.
    /// Calculating the price of an entity that somehow contains itself will likely hang.
    /// </remarks>
    public double GetPrice(EntityUid uid, bool includeContents = true)
    {
        double price = 0;
        //TODO: Add an OpaqueToAppraisal component or similar for blocking the recursive descent into containers, or preventing material pricing.
        // DO NOT FORGET TO UPDATE ESTIMATED PRICING
        price += GetMaterialsPrice(uid);
        price += GetSolutionsPrice(uid);

        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(uid);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(uid);
        }

        if (includeContents && TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    price += GetPrice(ent);
                }
            }
        }

        return price;
    }

    private double GetMaterialsPrice(EntityUid uid)
    {
        double price = 0;

        if (HasComp<MaterialComponent>(uid) &&
            TryComp<PhysicalCompositionComponent>(uid, out var composition))
        {
            var matPrice = GetMaterialPrice(composition);
            if (TryComp<StackComponent>(uid, out var stack))
                matPrice *= stack.Count;

            price += matPrice;
        }

        return price;
    }

    private double GetMaterialsPrice(EntityPrototype prototype)
    {
        double price = 0;

        if (prototype.Components.ContainsKey(_factory.GetComponentName(typeof(MaterialComponent))) &&
            prototype.Components.TryGetValue(_factory.GetComponentName(typeof(PhysicalCompositionComponent)), out var composition))
        {
            var compositionComp = (PhysicalCompositionComponent) composition.Component;
            var matPrice = GetMaterialPrice(compositionComp);

            if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackComponent)), out var stackProto))
            {
                matPrice *= ((StackComponent) stackProto.Component).Count;
            }

            price += matPrice;
        }

        return price;
    }

    private double GetSolutionsPrice(EntityUid uid)
    {
        var price = 0.0;

        if (TryComp<SolutionContainerManagerComponent>(uid, out var solComp))
        {
            price += GetSolutionPrice((uid, solComp));
        }

        return price;
    }

    private double GetSolutionsPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(SolutionContainerManagerComponent)), out var solManager))
        {
            var solComp = (SolutionContainerManagerComponent) solManager.Component;
            price += GetSolutionPrice(solComp);
        }

        return price;
    }

    private double GetStackPrice(EntityUid uid)
    {
        var price = 0.0;

        if (TryComp<StackPriceComponent>(uid, out var stackPrice) &&
            TryComp<StackComponent>(uid, out var stack) &&
            !HasComp<MaterialComponent>(uid)) // don't double count material prices
        {
            price += stack.Count * stackPrice.Price;
        }

        return price;
    }

    private double GetStackPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackPriceComponent)), out var stackpriceProto) &&
            prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackComponent)), out var stackProto) &&
            !prototype.Components.ContainsKey(_factory.GetComponentName(typeof(MaterialComponent))))
        {
            var stackPrice = (StackPriceComponent) stackpriceProto.Component;
            var stack = (StackComponent) stackProto.Component;
            price += stack.Count * stackPrice.Price;
        }

        return price;
    }

    private double GetStaticPrice(EntityUid uid)
    {
        var price = 0.0;

        if (TryComp<StaticPriceComponent>(uid, out var staticPrice))
        {
            price += staticPrice.Price;
        }

        return price;
    }

    private double GetStaticPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StaticPriceComponent)), out var staticProto))
        {
            var staticPrice = (StaticPriceComponent) staticProto.Component;
            price += staticPrice.Price;
        }

        return price;
    }

    // New Frontiers - Stack Vendor Prices - Gets overwrite values for vendor prices.
    // This code is licensed under AGPLv3. See AGPLv3.txt
    private double GetVendPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        // Prefer static price to stack price component, take the first positive value read.
        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StaticPriceComponent)), out var staticProto))
        {
            var staticComp = (StaticPriceComponent) staticProto.Component;
            if (staticComp.VendPrice > 0.0)
                price += staticComp.VendPrice;
        }
        if (price == 0.0 && prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackPriceComponent)), out var stackProto))
        {
            var stackComp = (StackPriceComponent) stackProto.Component;
            if (stackComp.VendPrice > 0.0)
                price += stackComp.VendPrice;
        }

        return price;
    }
    // End of modified code

    /// <summary>
    /// Appraises a grid, this is mainly meant to be used by yarrs.
    /// </summary>
    /// <param name="grid">The grid to appraise.</param>
    /// <param name="predicate">An optional predicate that controls whether or not the entity is counted toward the total.</param>
    /// <param name="afterPredicate">An optional predicate to run after the price has been calculated. Useful for high scores or similar.</param>
    /// <returns>The total value of the grid.</returns>
    public double AppraiseGrid(EntityUid grid, Func<EntityUid, bool>? predicate = null, Action<EntityUid, double>? afterPredicate = null)
    {
        var xform = Transform(grid);
        var price = 0.0;
        var enumerator = xform.ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (predicate is null || predicate(child))
            {
                var subPrice = GetPrice(child);
                price += subPrice;
                afterPredicate?.Invoke(child, subPrice);
            }
        }

        return price;
    }
}
