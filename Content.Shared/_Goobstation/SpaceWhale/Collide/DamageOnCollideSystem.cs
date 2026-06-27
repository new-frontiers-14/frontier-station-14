// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;


namespace Content.Shared._Goobstation.SpaceWhale.Collide;

public sealed class DamageOnCollideSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable  = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<DamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (!_whitelist.CheckBoth(args.OtherEntity, ent.Comp.IgnoreWhitelist, ent.Comp.Whitelist))
            return;
        var target = ent.Comp.Inverted ? args.OtherEntity : ent.Owner;
        _damageable.TryChangeDamage(target, ent.Comp.Damage);
    }
}
