// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    protected override void UpdateTailLayers(Entity<TailedEntityComponent> ent)
    {
        base.UpdateTailLayers(ent);

        if (_spriteQuery.TryGetComponent(ent.Owner, out var spriteSelf))
            spriteSelf.RenderOrder = (uint) ent.Comp.TailSegments.Count + 5;

        for (var i = 0; i < ent.Comp.TailSegments.Count; i++)
        {
            var segment = ent.Comp.TailSegments[i];

            if (TerminatingOrDeleted(segment))
                continue;

            if (!_spriteQuery.TryGetComponent(segment, out var sprite))
                continue;

            sprite.RenderOrder = (uint) (ent.Comp.TailSegments.Count - i + 5);
        }
    }
}
