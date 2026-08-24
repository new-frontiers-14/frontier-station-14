// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Destructible;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Goobstation.Containers.ExtendedContainer;

public sealed partial class ExtendedContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtendedContainerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ExtendedContainerComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<ExtendedContainerComponent, DestructionEventArgs>(OnBreak);
        SubscribeLocalEvent<ExtendedContainerComponent, ContainerIsInsertingAttemptEvent>(OnContainerIsInsertingAttempt);
        SubscribeLocalEvent<ExtendedContainerComponent, ContainerIsRemovingAttemptEvent>(OnContainerIsRemovingAttempt);
        SubscribeLocalEvent<ExtendedContainerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
    }

    private void OnComponentInit(EntityUid uid, ExtendedContainerComponent component, ComponentInit args)
    {
        component.Content = _containers.EnsureContainer<Container>(uid, component.ContainerName);
    }

    private void OnBreak(EntityUid uid, ExtendedContainerComponent component, EntityEventArgs args)
    {
        if (component.DeleteContentsOnBreak)
            return;

        if (component.Content == null)
            return;

        var coords = Transform(uid).Coordinates;

        foreach (var entity in component.Content.ContainedEntities.ToArray())
        {
            _containers.Remove(entity, component.Content);
            _transform.SetCoordinates(entity, coords);
        }
    }

    private void OnContainerIsRemovingAttempt(EntityUid uid, ExtendedContainerComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID != component.ContainerName)
            return;

        if (component.RemoveWhitelist != null && _whitelistSystem.IsValid(component.RemoveWhitelist, args.EntityUid))
        {
            args.Cancel();
            return;
        }

        if (component.RemoveSound != null)
            _audioSystem.PlayPredicted(component.RemoveSound, Transform(uid).Coordinates, uid);
    }

    private void OnContainerIsInsertingAttempt(EntityUid uid, ExtendedContainerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != component.ContainerName)
            return;

        var isContainerFull = component.Content?.ContainedEntities.Count >= component.Capacity;

        if (isContainerFull ||
            component.InsertWhitelist != null &&
            _whitelistSystem.IsValid(component.InsertWhitelist, args.EntityUid))
        {
            args.Cancel();
            return;
        }

        if (component.InsertSound != null)
            _audioSystem.PlayPredicted(component.InsertSound, Transform(uid).Coordinates, uid);
    }
    private void OnGetVerb(
        EntityUid uid,
        ExtendedContainerComponent component,
        GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        AlternativeVerb ejectVerb = new()
        {
            Text = "Eject Contents",
            Category = VerbCategory.Eject,
            Act = () => EjectContents(uid, component)
        };

        args.Verbs.Add(ejectVerb);
    }
    private void EjectContents(EntityUid uid, ExtendedContainerComponent component)
    {
        if (component.Content == null)
            return;

        var coords = Transform(uid).Coordinates;

        foreach (var entity in component.Content.ContainedEntities.ToArray())
        {
            _containers.Remove(entity, component.Content);
            _transform.SetCoordinates(entity, coords);
        }
    }
}
