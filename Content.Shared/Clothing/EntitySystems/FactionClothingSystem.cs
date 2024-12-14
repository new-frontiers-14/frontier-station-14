using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Player; // Frontier - Dont edit AI factions
using Content.Shared.Inventory; // Frontier
using Content.Shared.NPC.Prototypes; // Frontier
using Robust.Shared.Prototypes; // Frontier
using Content.Shared.Mind.Components; // Frontier

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles <see cref="FactionClothingComponent"/> faction adding and removal.
/// </summary>
public sealed class FactionClothingSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<FactionClothingComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<NpcFactionMemberComponent, PlayerAttachedEvent>(OnPlayerAttached); // Frontier
        SubscribeLocalEvent<NpcFactionMemberComponent, PlayerDetachedEvent>(OnPlayerDetached); // Frontier
    }

    // Frontier: rewritten from scratch
    private void OnEquipped(Entity<FactionClothingComponent> ent, ref GotEquippedEvent args)
    {
        var alreadyMember = CheckEntityEquipmentForFaction(args.Equipee, ent.Comp.Faction, args.Equipment);
        if (alreadyMember is null)
        {
            TryComp<NpcFactionMemberComponent>(args.Equipee, out var factionComp);
            var faction = (args.Equipee, factionComp);
            ent.Comp.AlreadyMember = _faction.IsMember(faction, ent.Comp.Faction);

            // Do not edit factions on AI controlled mobs
            if (!HasComp<ActorComponent>(args.Equipee))
                return;

            if (!ent.Comp.AlreadyMember)
                _faction.AddFaction(faction, ent.Comp.Faction);
        }
        else
        {
            ent.Comp.AlreadyMember = alreadyMember.Value;
        }
    }

    private void OnUnequipped(Entity<FactionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        // Reset the component, should be false when unworn.
        if (ent.Comp.AlreadyMember)
        {
            ent.Comp.AlreadyMember = false;
            return;
        }

        // Do not edit factions on AI controlled mobs
        if (!HasComp<ActorComponent>(args.Equipee))
            return;

        var alreadyMember = CheckEntityEquipmentForFaction(args.Equipee, ent.Comp.Faction, args.Equipment);
        if (alreadyMember is null)
        {
            _faction.RemoveFaction(args.Equipee, ent.Comp.Faction);
        }
    }

    public bool? CheckEntityEquipmentForFaction(EntityUid ent, ProtoId<NpcFactionPrototype> prototype, EntityUid? skipEnt = null)
    {
        var enumerator = _inventory.GetSlotEnumerator(ent);
        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<FactionClothingComponent>(item, out var faction))
                continue;
            if (faction.Faction == prototype && item != skipEnt)
                return faction.AlreadyMember;
        }
        return null;
    }

    private void OnPlayerAttached(Entity<NpcFactionMemberComponent> ent, ref PlayerAttachedEvent args)
    {
        // Iterate through all items, add factions for any items found where AlreadyMember is false
        List<ProtoId<NpcFactionPrototype>> factions = new();
        var enumerator = _inventory.GetSlotEnumerator(ent.Owner);
        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<FactionClothingComponent>(item, out var faction))
                continue;
            if (!faction.AlreadyMember && !factions.Contains(faction.Faction))
            {
                _faction.AddFaction((ent.Owner, ent.Comp), faction.Faction);
                factions.Add(faction.Faction);
            }
        }
    }

    private void OnPlayerDetached(Entity<NpcFactionMemberComponent> ent, ref PlayerDetachedEvent args)
    {
        // Iterate through all items, remove factions for any items found where AlreadyMember is true
        List<ProtoId<NpcFactionPrototype>> factions = new();
        var enumerator = _inventory.GetSlotEnumerator(ent.Owner);
        while (enumerator.NextItem(out var item))
        {
            if (!TryComp<FactionClothingComponent>(item, out var faction))
                continue;
            if (!faction.AlreadyMember && !factions.Contains(faction.Faction))
            {
                _faction.RemoveFaction((ent.Owner, ent.Comp), faction.Faction);
                factions.Add(faction.Faction);
            }
        }
    }
    // End Frontier
}
