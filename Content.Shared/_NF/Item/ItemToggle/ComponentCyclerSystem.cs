using Content.Shared._NF.Item.ItemToggle.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Item.ItemToggle;

/// <summary>
/// Handles <see cref="ComponentCyclerComponent"/> component manipulation.
/// </summary>
public sealed class ComponentCyclerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<ComponentCyclerComponent, ItemToggledEvent>(OnCycle);

        SubscribeLocalEvent<ComponentCyclerComponent, ComponentStartup>(OnComponentCyclerStartup);
        SubscribeLocalEvent<ComponentCyclerComponent, ActivateInWorldEvent>(OnComponentCyclerActivated);
        SubscribeLocalEvent<ComponentCyclerComponent, AfterAutoHandleStateEvent>(OnComponentCyclerHandleState);
    }

    private void OnComponentCyclerHandleState(Entity<ComponentCyclerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SetComponentCyclerInternal(ent, ent.Comp.CurrentEntry);
    }

    private void OnComponentCyclerStartup(Entity<ComponentCyclerComponent> ent, ref ComponentStartup args)
    {
        SetComponentCyclerInternal(ent, ent.Comp.CurrentEntry);
    }

    private void OnComponentCyclerActivated(Entity<ComponentCyclerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = CycleComponentCyclerInternal(ent, args.User);
    }

    public bool CycleComponentCycler(Entity<ComponentCyclerComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;
        return CycleComponentCyclerInternal((ent.Owner, ent.Comp), user);
    }

    public void SetComponentCycler(Entity<ComponentCyclerComponent?> ent,
        int newIndex,
        ComponentRegistry? comp = null,
        bool playSound = false,
        EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        SetComponentCyclerInternal((ent.Owner, ent.Comp), newIndex, comp, playSound, user);
    }

    private bool CycleComponentCyclerInternal(Entity<ComponentCyclerComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Entries.Length == 0)
            return false;

        var newIndex = ((ent.Comp.CurrentEntry + 1) % ent.Comp.Entries.Length);
        SetComponentCyclerInternal(ent, newIndex, playSound: true, user: user);

        return true;
    }

    public void SetComponentCyclerInternal(Entity<ComponentCyclerComponent> ent,
        int newIndex,
        ComponentRegistry? comp = null,
        bool playSound = false,
        EntityUid? user = null)
    {
        if (newIndex >= ent.Comp.Entries.Length)
            return;

        // Remove previous components if index is within bounds
        if (ent.Comp.CurrentEntry < ent.Comp.Entries.Length)
        {
            var previous = ent.Comp.Entries[ent.Comp.CurrentEntry];

            EntityManager.RemoveComponents(ent, previous.Components);
        }

        // Update previous entry
        ent.Comp.CurrentEntry = newIndex;
        var current = ent.Comp.Entries[ent.Comp.CurrentEntry];

        // Add new components
        EntityManager.AddComponents(ent, current.Components);

        if (playSound && current.ChangeSound != null)
            _audioSystem.PlayPredicted(current.ChangeSound, ent, user);
    }
}
