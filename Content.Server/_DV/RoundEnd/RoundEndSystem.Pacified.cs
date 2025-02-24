using Content.Server.Explosion.Components;
using Content.Server.GameTicking;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared._DV.CCVars;
using Content.Shared.Explosion.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Store.Components;
using Robust.Shared.Configuration;

namespace Content.Server._DV.RoundEnd;

public sealed class PacifiedRoundEnd : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        _configurationManager.OnValueChanged(DCCVars.RoundEndPacifist, v => _enabled = v, true);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        if (!_enabled)
            return;

        var harmQuery = EntityQueryEnumerator<CombatModeComponent>();
        while (harmQuery.MoveNext(out var uid, out _))
        {
            EnsureComp<PacifiedComponent>(uid);
        }

        var explosiveQuery = EntityQueryEnumerator<ExplosiveComponent>();
        while (explosiveQuery.MoveNext(out var uid, out _))
        {
            RemComp<ExplosiveComponent>(uid);
        }

        var grenadeQuery = EntityQueryEnumerator<OnUseTimerTriggerComponent>();
        while (grenadeQuery.MoveNext(out var uid, out _))
        {
            RemComp<OnUseTimerTriggerComponent>(uid);
        }

        var flashQuery = EntityQueryEnumerator<FlashComponent>();
        while (flashQuery.MoveNext(out var uid, out _))
        {
            RemComp<FlashComponent>(uid);
        }

        var uplinkQuery = EntityQueryEnumerator<StoreComponent>();
        while (uplinkQuery.MoveNext(out var uid, out _))
        {
            RemComp<StoreComponent>(uid);
        }
    }
}
