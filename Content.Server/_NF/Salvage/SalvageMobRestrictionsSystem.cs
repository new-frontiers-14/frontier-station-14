using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Server.Body.Systems;

namespace Content.Server._NF.Salvage;

public sealed class SalvageMobRestrictionsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMobRestrictionsNFComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SalvageMobRestrictionsNFComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SalvageMobRestrictionsGridComponent, ComponentRemove>(OnRemoveGrid);
    }

    private void OnInit(EntityUid uid, SalvageMobRestrictionsNFComponent component, ComponentInit args)
    {
        var gridUid = Transform(uid).ParentUid;
        if (!EntityManager.EntityExists(gridUid))
        {
            // Give up, we were spawned improperly
            return;
        }
        // When this code runs, the salvage magnet hasn't actually gotten ahold of the entity yet.
        // So it therefore isn't in a position to do this.
        if (!TryComp(gridUid, out SalvageMobRestrictionsGridComponent? rg))
        {
            rg = AddComp<SalvageMobRestrictionsGridComponent>(gridUid);
        }
        rg!.MobsToKill.Add(uid);
        component.LinkedGridEntity = gridUid;
    }

    private void OnRemove(EntityUid uid, SalvageMobRestrictionsNFComponent component, ComponentRemove args)
    {
        if (TryComp(component.LinkedGridEntity, out SalvageMobRestrictionsGridComponent? rg))
        {
            rg.MobsToKill.Remove(uid);
        }
    }

    private void OnRemoveGrid(EntityUid uid, SalvageMobRestrictionsGridComponent component, ComponentRemove args)
    {
        foreach (EntityUid target in component.MobsToKill)
        {
            if (TryComp(target, out BodyComponent? body))
            {
                // Just because.
                var gibs = _body.GibBody(target, body: body, gibOrgans: true);
                foreach (var gib in gibs)
                    Del(gib);
            }
            else if (TryComp(target, out DamageableComponent? dc))
            {
                _damageableSystem.SetAllDamage(target, dc, 200);
            }
        }
    }
}

