// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.SpaceWhale;
using Robust.Client.GameObjects;

namespace Content.Client._Goobstation.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{

    protected override void UpdateTailLayers(Entity<TailedEntityComponent> ent)
    {
        var spriteQuery = GetEntityQuery<SpriteComponent>();
        base.UpdateTailLayers(ent);

        if (spriteQuery.TryGetComponent(ent.Owner, out var spriteSelf))
            spriteSelf.RenderOrder = (uint) ent.Comp.TailSegments.Count + 5;

        for (var i = 0; i < ent.Comp.TailSegments.Count; i++)
        {
            var segment = ent.Comp.TailSegments[i];

            if (TerminatingOrDeleted(segment))
                continue;

            if (!spriteQuery.TryGetComponent(segment, out var sprite))
                continue;

            sprite.RenderOrder = (uint) (ent.Comp.TailSegments.Count - i + 5);
        }
    }
}
