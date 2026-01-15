using System.Linq;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server._Starlight.Medical.Limbs;

public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeLimbWithItems()
    {
        base.Initialize();
        SubscribeLocalEvent<LimbWithItemsComponent, ComponentInit>(OnLimbWithItemsInit);
        SubscribeLocalEvent<LimbWithItemsComponent, ToggleLimbEvent>(OnLimbToggle);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<LimbWithItemsComponent>>(LimbWithItemsRemoved);
    }

    private void LimbWithItemsRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<LimbWithItemsComponent> args)
    {
        if (!args.Comp.Toggled)
            return;

        var toggleLimbEvent = new ToggleLimbEvent
        {
            Performer = ent.Owner,
        };
        OnLimbToggle((args.Limb, args.Comp), ref toggleLimbEvent);
    }

    private void OnLimbToggle(Entity<LimbWithItemsComponent> ent, ref ToggleLimbEvent args)
    {
        ent.Comp.Toggled = !ent.Comp.Toggled;

        if (ent.Comp.Toggled)
        {
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                var hands = EnsureComp<HandsComponent>(args.Performer);
                _hands.AddHand((args.Performer, hands), handId, HandLocation.Middle);
                _hands.DoPickup(args.Performer, handId, item, hands);
                EnsureComp<UnremoveableComponent>(item);
            }
        }
        else
        {
            var container = _container.EnsureContainer<Container>(ent.Owner, "cyberlimb", out _);
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                RemComp<UnremoveableComponent>(item);
                var hands = EnsureComp<HandsComponent>(args.Performer);
                var toInsert = (item,
                    CompOrNull<TransformComponent>(item),
                    CompOrNull<MetaDataComponent>(item),
                    CompOrNull<PhysicsComponent>(item));
                _container.Insert(toInsert, container, force: true);
                _hands.RemoveHand(args.Performer, handId);
            }
        }

        if (TryComp(ent.Owner, out BaseLayerIdComponent? baseLayer) &&
            TryComp(ent.Owner, out BaseLayerIdToggledComponent? toggledLayer) &&
            TryComp(ent.Owner, out BodyPartComponent? limbPart) &&
            TryComp(args.Performer, out HumanoidAppearanceComponent? performer))
        {
            _limb.ToggleLimbVisual(
                (args.Performer, performer),
                (ent.Owner, baseLayer, toggledLayer, limbPart),
                ent.Comp.Toggled);
        }

        _audio.PlayPvs(ent.Comp.Sound, args.Performer);

        Dirty(ent);
    }

    private void OnLimbWithItemsInit(Entity<LimbWithItemsComponent> limb, ref ComponentInit args)
    {
        if (limb.Comp.ItemEntities?.Count == limb.Comp.Items.Count)
            return;
        var container = _container.EnsureContainer<Container>(limb.Owner, "cyberlimb", out _);

        limb.Comp.ItemEntities = [.. limb.Comp.Items.Select(EnsureItem)];

        DirtyEntity(limb);

        EntityUid EnsureItem(EntProtoId proto)
        {
            var id = Spawn(proto);
            var toInsert = (id,
                CompOrNull<TransformComponent>(id),
                CompOrNull<MetaDataComponent>(id),
                CompOrNull<PhysicsComponent>(id));
            _container.Insert(toInsert, container, force: true);
            return id;
        }
    }
}
