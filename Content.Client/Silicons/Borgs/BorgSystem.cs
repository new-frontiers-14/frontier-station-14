﻿using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client.Silicons.Borgs;

/// <inheritdoc/>
public sealed class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, AppearanceChangeEvent>(OnBorgAppearanceChanged);
        SubscribeLocalEvent<MMIComponent, AppearanceChangeEvent>(OnMMIAppearanceChanged);
    }

    private void OnBorgAppearanceChanged(EntityUid uid, BorgChassisComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        UpdateBorgAppearnce(uid, component, args.Component, args.Sprite);
    }

    protected override void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        base.OnInserted(uid, component, args);
        UpdateBorgAppearnce(uid, component);
    }

    protected override void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {
        if (!component.Initialized)
            return;

        base.OnRemoved(uid, component, args);
        UpdateBorgAppearnce(uid, component);
    }

    private void UpdateBorgAppearnce(EntityUid uid,
        BorgChassisComponent? component = null,
        AppearanceComponent? appearance = null,
        SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref appearance, ref sprite))
            return;

        if (!_appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasPlayer, appearance))
            hasPlayer = false;

        sprite.LayerSetVisible(BorgVisualLayers.Light, component.BrainEntity != null || hasPlayer);
        sprite.LayerSetState(BorgVisualLayers.Light, hasPlayer ? component.HasMindState : component.NoMindState);
    }

    private void OnMMIAppearanceChanged(EntityUid uid, MMIComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        var sprite = args.Sprite;

        if (!_appearance.TryGetData(uid, MMIVisuals.BrainPresent, out bool brain))
            brain = false;
        if (!_appearance.TryGetData(uid, MMIVisuals.HasMind, out bool hasMind))
            hasMind = false;

        sprite.LayerSetVisible(MMIVisualLayers.Brain, brain);
        if (!brain)
        {
            sprite.LayerSetState(MMIVisualLayers.Base, component.NoBrainState);
        }
        else
        {
            var state = hasMind
                ? component.HasMindState
                : component.NoMindState;
            sprite.LayerSetState(MMIVisualLayers.Base, state);
        }
    }
}
