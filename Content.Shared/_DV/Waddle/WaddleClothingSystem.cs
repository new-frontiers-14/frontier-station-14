using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Alert;
using Content.Shared.Inventory; //imp edit
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Timing; //imp edit

namespace Content.Shared._DV.Waddle;

public sealed class WaddleClothingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!; //imp edit
    [Dependency] private readonly IGameTiming _timing = default!; //imp edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ItemToggledEvent>(OnToggled); //imp edit, waddle toggling
    }

    private void OnGotEquipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotEquippedEvent args)
    {
        // imp edit, return out of method if it is not the first time predicting to avoid log spam
        // then, check if the item has a ToggleComponent. if so, do not add the waddling animation to the wearer if it is no activated
        if ((!_timing.IsFirstTimePredicted) || (TryComp<ItemToggleComponent>(ent, out var itemToggle) && (!itemToggle.Activated)))
            return;
        var user = args.Wearer;
        // imp edit, code moved to its own method
        AddWaddleAnimationComponent(ent, user);
    }

    private void OnGotUnequipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // imp edit, code moved to its own method
        RemoveWaddleAnimationComponent(ent, args.Wearer);
    }

    // imp edit, allows waddling to be toggled through an action
    private void OnToggled(Entity<WaddleWhenWornComponent> ent, ref ItemToggledEvent args)
    {
        if (args.User is null)
            return;
        var user = args.User.Value;
        if (args.Activated)
            AddWaddleAnimationComponent(ent, user);
        else
            RemoveWaddleAnimationComponent(ent, user);
    }

    // imp edit, code block moved from OnGotEquipped to this method, since it's used in multiple methods
    private void AddWaddleAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid user)
    {
        // TODO: refcount
        if (EnsureComp<WaddleAnimationComponent>(user, out var waddle))
            return;

        ent.Comp.AddedWaddle = true;
        Dirty(ent);

        var comp = ent.Comp;
        if (comp.AnimationLength is { } length)
            waddle.AnimationLength = length;
        if (comp.HopIntensity is { } hopIntensity)
            waddle.HopIntensity = hopIntensity;
        if (comp.TumbleIntensity is { } tumbleIntensity)
            waddle.TumbleIntensity = tumbleIntensity;
        if (comp.RunAnimationLengthMultiplier is { } multiplier)
            waddle.RunAnimationLengthMultiplier = multiplier;

        // very unlikely that some waddle clothing doesn't change at least 1 property, don't bother doing change detection meme
        Dirty(user, waddle);
        //imp edit, add waddle alert if one is defined
        if (comp.WaddlingAlert is { } alert)
            _alerts.ShowAlert(user, alert);
    }

    // imp edit, code block moved from OnGotUnequipped to this method, since it's used in multiple methods
    private void RemoveWaddleAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid user)
    {
        if (!ent.Comp.AddedWaddle)
            return;

        // TODO: refcount
        RemComp<WaddleAnimationComponent>(user);
        ent.Comp.AddedWaddle = false;
        Dirty(ent);
        //imp edit, clear waddle alert if one is defined
        if (ent.Comp.WaddlingAlert is { } alert)
            _alerts.ClearAlert(user, alert);
    }
}
