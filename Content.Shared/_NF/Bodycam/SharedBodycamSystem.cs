using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Bodycam;

public abstract class SharedBodycamSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly ClothingSystem _clothingSys = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BodycamComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnInit(EntityUid uid, BodycamComponent component, ComponentInit args)
    {
        UpdateVisuals(uid, component);

        // Want to make sure client has latest data on level so battery displays properly.
        Dirty(component);
    }

    private void OnHandleState(EntityUid uid, BodycamComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BodycamComponent.BodycamComponentState state)
            return;

        SetActivated(uid, state.Activated, component, false);
    }

    public void SetActivated(EntityUid uid, bool activated, BodycamComponent? component = null, bool makeNoise = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Activated == activated)
            return;

        component.Activated = activated;

        if (makeNoise)
        {
            var sound = component.Activated ? component.TurnOnSound : component.TurnOffSound;
            _audio.PlayPvs(sound, component.Owner);
        }

        Dirty(component);
        UpdateVisuals(uid, component);
    }

    public void UpdateVisuals(EntityUid uid, BodycamComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        if (component.AddPrefix)
        {
            var prefix = component.Activated ? "on" : "off";
            _itemSys.SetHeldPrefix(uid, prefix);
            _clothingSys.SetEquippedPrefix(uid, prefix);
        }

        if (component.ToggleAction != null)
            _actionSystem.SetToggled(component.ToggleAction, component.Activated);

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, component.Activated, appearance);
    }
}
