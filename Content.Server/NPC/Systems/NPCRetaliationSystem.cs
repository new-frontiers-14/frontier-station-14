﻿using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles NPC which become aggressive after being attacked.
/// </summary>
public sealed class NPCRetaliationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    private readonly HashSet<EntityUid> _deAggroQueue = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NPCRetaliationComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<NPCRetaliationComponent, DisarmedEvent>(OnDisarmed);
    }

    private void OnDamageChanged(EntityUid uid, NPCRetaliationComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (args.Origin is not { } origin)
            return;

        TryRetaliate(uid, origin, component);
    }

    private void OnDisarmed(EntityUid uid, NPCRetaliationComponent component, DisarmedEvent args)
    {
        TryRetaliate(uid, args.Source, component);
    }

    public bool TryRetaliate(EntityUid uid, EntityUid target, NPCRetaliationComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // don't retaliate against inanimate objects.
        if (!HasComp<MobStateComponent>(target))
            return false;

        if (_npcFaction.IsEntityFriendly(uid, target))
            return false;

        _npcFaction.AggroEntity(uid, target);
        if (component.AttackMemoryLength is { } memoryLength)
        {
            component.AttackMemories[target] = _timing.CurTime + memoryLength;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCRetaliationComponent, FactionExceptionComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var factionException, out var metaData))
        {
            _deAggroQueue.Clear();

            foreach (var ent in new ValueList<EntityUid>(comp.AttackMemories.Keys))
            {
                if (_timing.CurTime < comp.AttackMemories[ent])
                    continue;

                if (TerminatingOrDeleted(ent, metaData))
                    _deAggroQueue.Add(ent);

                _deAggroQueue.Add(ent);
            }

            foreach (var ent in _deAggroQueue)
            {
                _npcFaction.DeAggroEntity(uid, ent, factionException);
            }
        }
    }
}
