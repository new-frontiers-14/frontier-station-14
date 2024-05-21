using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Corvax.Language.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Corvax.Language.EntitySystems;

public sealed class LanguageTranslatorSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _power = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageTranslatorComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<LanguageTranslatorComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<LanguageTranslatorComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<LanguageTranslatorComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);

        SubscribeLocalEvent<BatteryComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<BatteryComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<BatteryComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnActivateInWorld(EntityUid entity, LanguageTranslatorComponent component, ActivateInWorldEvent e)
    {
        if (e.Handled)
            return;

        Toggle(entity, component);

        e.Handled = true;
    }

    private void OnGetItemActions(EntityUid entity, LanguageTranslatorComponent component, GetItemActionsEvent e)
    {
        e.AddAction(ref component.ToggleActionEntity, "ActionToggleTranslator");
    }

    private void OnToggleAction(EntityUid entity, LanguageTranslatorComponent component, ToggleActionEvent e)
    {
        if (e.Handled)
            return;

        Toggle(entity, component);

        e.Handled = true;
    }

    private void OnGetVerbs(EntityUid entity, LanguageTranslatorComponent component, GetVerbsEvent<ActivationVerb> e)
    {
        if (e.CanAccess && e.CanInteract)
            e.Verbs.Add(new()
            {
                Text = Loc.GetString("verb-toggle-translator"),
                Act = () => Toggle(entity, component)
            });
    }

    private void Toggle(EntityUid entity, LanguageTranslatorComponent component)
    {
        component.Activated = !component.Activated;

        UpdateVisuals(new(entity, component));
    }

    private void OnChargeChanged(EntityUid entity, BatteryComponent component, ChargeChangedEvent e)
    {
        TryUpdateVisuals(Transform(entity).ParentUid);
    }

    private void OnInsert(EntityUid entity, BatteryComponent component, EntGotInsertedIntoContainerMessage e)
    {
        TryUpdateVisuals(e.Container.Owner);
    }

    private void OnRemove(EntityUid entity, BatteryComponent component, EntGotRemovedFromContainerMessage e)
    {
        TryUpdateVisuals(e.Container.Owner);
    }

    private void TryUpdateVisuals(EntityUid entity)
    {
        if (TryComp<LanguageTranslatorComponent>(entity, out var translatorComponent))
            UpdateVisuals(new(entity, translatorComponent));
    }

    public bool TryUseTranslator(EntityUid entity, string message)
    {
        if (!TryGetTranslator(_inventory.GetHandOrInventoryEntities(entity), out var translator) || !translator.Value.Comp.Activated)
            return false;

        if (_power.TryUseCharge(translator.Value, 0.2f * message.Length))
            return true;

        if (_power.TryGetBatteryFromSlot(translator.Value, out var battery, out var batteryComponent))
            _battery.SetCharge(battery.Value, 0, batteryComponent);

        return false;
    }

    private bool TryGetTranslator(IEnumerable<EntityUid> entities, [NotNullWhen(true)] out Entity<LanguageTranslatorComponent>? translator)
    {
        foreach (var entity in entities)
        {
            if (EntityManager.TryGetComponent<LanguageTranslatorComponent>(entity, out var component))
            {
                translator = new(entity, component);
                return true;
            }

            if (EntityManager.TryGetComponent<StorageComponent>(entity, out var storage) && TryGetTranslator(storage.Container.ContainedEntities, out translator))
                return true;
        }

        translator = null;
        return false;
    }

    public void UpdateVisuals(Entity<LanguageTranslatorComponent> entity)
    {
        _appearance.SetData(entity, LanguageTranslatorVisuals.Enabled, entity.Comp.Activated && _power.TryGetBatteryFromSlot(entity, out var battery) && battery.CurrentCharge > 0);
    }
}
