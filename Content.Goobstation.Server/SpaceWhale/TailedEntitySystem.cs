// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;
using Robust.Shared.Map;

namespace Content.Goobstation.Server.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TailedEntityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TailedEntityComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnMapInit(Entity<TailedEntityComponent> ent, ref MapInitEvent args)
    {
        InitializeTailSegments(ent);
    }

    private void OnComponentShutdown(Entity<TailedEntityComponent> ent, ref ComponentShutdown args)
    {
        foreach (var segment in ent.Comp.TailSegments)
        {
            if (!TerminatingOrDeleted(segment))
                QueueDel(segment);
        }
    }

    private void InitializeTailSegments(Entity<TailedEntityComponent> ent, TransformComponent? xform = null)
    {
        if (!Resolve(ent.Owner, ref xform))
            return;

        var mapUid = xform.MapUid;
        if (mapUid == null)
            return;

        var (headPos, headRot) = TransformSystem.GetWorldPositionRotation(xform);

        for (var i = 0; i < ent.Comp.Amount; i++)
        {
            var offset = headRot.ToWorldVec() * ent.Comp.Spacing * (i + 1);
            var spawnPos = headPos - offset;

            var segment = Spawn(ent.Comp.Prototype, new EntityCoordinates(mapUid.Value, spawnPos));
            ent.Comp.TailSegments.Add(segment);
        }

        Dirty(ent);
    }
}
