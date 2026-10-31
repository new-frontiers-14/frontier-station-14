// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Clothing.Components;
using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Shared._Goobstation.Clothing.Systems
{
    public sealed partial class DamageOverTimeSystem : EntitySystem
    {
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private DamageableSystem _damageSys = default!;

        public override void Update(float frameTime)
        {
            var currentTime = _timing.CurTime;
            var query = EntityQueryEnumerator<DamageOverTimeComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                if (currentTime < component.NextTickTime)
                    continue;
                component.NextTickTime = currentTime + component.Interval;
                _damageSys.TryChangeDamage(uid, component.Damage, ignoreResistances: component.IgnoreResistances);
            }
        }
    }
}
