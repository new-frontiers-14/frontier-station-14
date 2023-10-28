using Content.Shared.Interaction.Events;
using Content.Shared._Park.Species.Shadowkin.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinDarken : EntitySystem
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinDarkSwappedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }


    private void OnInteractionAttempt(EntityUid uid, ShadowkinDarkSwappedComponent component, InteractionAttemptEvent args)
    {
        if (args.Target == null || !_entity.TryGetComponent<TransformComponent>(args.Target, out var __) ||
            _entity.TryGetComponent<ShadowkinDarkSwappedComponent>(args.Target, out _))
            return;

        args.Cancel();
        if (_gameTiming.InPrediction)
            return;

        _popup.PopupEntity(Loc.GetString("ethereal-pickup-fail"), args.Target.Value, uid);
    }
}
