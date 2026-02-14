using Content.Shared._CitadelStation.ERP.Interaction.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;
using Content.Shared.Chat;
using Robust.Shared.Random;

namespace Content.Shared._CitadelStation.ERP.Interaction.Systems;

public sealed class SharedArousalSystem : EntitySystem {
    //[Dependency] private readonly EntityEffectSystem _entityEffects = default!;
    //[Dependency] private readonly PopupSystem _popupSystem = default!;
    //[Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTimingSystem = default!;

    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private ISawmill _sawmill = default!;
    private readonly FixedPoint2 _arousalMultiplier = 0.1f;
    private FixedPoint2 _currentMoanChance;

    public override void Initialize() {
        base.Initialize();
        _sawmill = Logger.GetSawmill("Arousal");
    }

    public override void Update(float frameTime){

        var curTime = _gameTimingSystem.CurTime;

        var entityQuery = EntityQueryEnumerator<ERPArousalComponent>();
        while (entityQuery.MoveNext(out var uid, out var arousedComponent)) {

            if (arousedComponent.NextUpdate > curTime)
                continue;
            if (arousedComponent.ArousalBaseline - arousedComponent.CurrentArousal != 0.0f) {
                if (arousedComponent.ArousalBaseline - arousedComponent.CurrentArousal <= FixedPoint2.Epsilon) {
                    arousedComponent.CurrentArousal = arousedComponent.ArousalBaseline;
                } else {
                    arousedComponent.CurrentArousal += (arousedComponent.ArousalBaseline - arousedComponent.CurrentArousal) * _arousalMultiplier;
                }
            }
            arousedComponent.NextUpdate = curTime + arousedComponent.UpdateFrequency;
            Dirty(uid, arousedComponent);
        }
    }
};
