using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public abstract class ClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string HairTag = "HidesHair";
    private const string HeadTopTag = "HidesHeadTop"; // Frontier
    private const string TailTag = "HidesTail"; // Frontier
    private const string SnoutTag = "HidesSnout"; // Frontier

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    protected virtual void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        component.InSlot = args.Slot;
        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HairTag))
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, false);

        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HeadTopTag)) // Frontier
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.HeadTop, false); // Frontier

        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, SnoutTag)) // Frontier
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Snout, false); // Frontier

        if (TryComp<HumanoidAppearanceComponent>(args.Equipee, out var specie)) // Frontier
        {
            string[] speciesToProcess = { "Vulpkanin", "Reptilian", "Felinid" };

            // Check if the current species is in the list
            if (Array.Exists(speciesToProcess, s => s.Equals(specie?.Species, StringComparison.OrdinalIgnoreCase)))
            {
                if (args.Slot == "outerClothing" && _tagSystem.HasTag(args.Equipment, TailTag))
                    _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Tail, false);
            }
        }
    }

    protected virtual void OnGotUnequipped(EntityUid uid, ClothingComponent component, GotUnequippedEvent args)
    {
        component.InSlot = null;
        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HairTag))
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Hair, true);

        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, HeadTopTag)) // Frontier
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.HeadTop, true); // Frontier

        if (args.Slot == "head" && _tagSystem.HasTag(args.Equipment, SnoutTag)) // Frontier
            _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Snout, true); // Frontier

        if (TryComp<HumanoidAppearanceComponent>(args.Equipee, out var specie)) // Frontier
        {
            string[] speciesToProcess = { "Vulpkanin", "Reptilian", "Felinid" };

            // Check if the current species is in the list
            if (Array.Exists(speciesToProcess, s => s.Equals(specie?.Species, StringComparison.OrdinalIgnoreCase)))
            {
                if (args.Slot == "outerClothing" && _tagSystem.HasTag(args.Equipment, TailTag))
                    _humanoidSystem.SetLayerVisibility(args.Equipee, HumanoidVisualLayers.Tail, false);
            }
        }
    }

    private void OnGetState(EntityUid uid, ClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingComponentState(component.EquippedPrefix);
    }

    private void OnHandleState(EntityUid uid, ClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingComponentState state)
            SetEquippedPrefix(uid, state.EquippedPrefix, component);
    }

    #region Public API

    public void SetEquippedPrefix(EntityUid uid, string? prefix, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing, false))
            return;

        if (clothing.EquippedPrefix == prefix)
            return;

        clothing.EquippedPrefix = prefix;
        _itemSys.VisualsChanged(uid);
        Dirty(clothing);
    }

    public void SetSlots(EntityUid uid, SlotFlags slots, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.Slots = slots;
        Dirty(clothing);
    }

    /// <summary>
    ///     Copy all clothing specific visuals from another item.
    /// </summary>
    public void CopyVisuals(EntityUid uid, ClothingComponent otherClothing, ClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.ClothingVisuals = otherClothing.ClothingVisuals;
        clothing.EquippedPrefix = otherClothing.EquippedPrefix;
        clothing.RsiPath = otherClothing.RsiPath;
        clothing.FemaleMask = otherClothing.FemaleMask;

        _itemSys.VisualsChanged(uid);
        Dirty(clothing);
    }

    #endregion
}
