using System.Diagnostics.CodeAnalysis;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Corvax.Language.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;

namespace Content.Server.Corvax.Language.EntitySystems;

public sealed class LanguageTranslatorSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PowerCellSystem _power = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageTranslatorComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<LanguageTranslatorComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<LanguageTranslatorComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<LanguageTranslatorComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
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

        _appearance.SetData(entity, LanguageTranslatorVisuals.Activated, component.Activated);
    }

    public bool TryUseTranslator(EntityUid entity, string message)
    {
        return TryGetTranslator(_inventory.GetHandOrInventoryEntities(entity), out var translator) && translator.Value.Comp.Activated && _power.TryUseCharge(translator.Value, 0.2f * message.Length);
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
}
