using Content.Shared._CitadelStation.ERP.Interaction.Components;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared._CitadelStation.ERP.Interaction.Systems;

public abstract class SharedArousalSystem : EntitySystem {
    //[Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTimingSystem = default!;

    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    protected ISawmill _sawmill = default!;
    private readonly FixedPoint2 _arousalMultiplier = 0.1f;
    private FixedPoint2 _currentMoanChance;

    public override void Initialize() {
        base.Initialize();
        _sawmill = Logger.GetSawmill("ArousalShared");
    }

    public override void Update(float frameTime){
        var curTime = _gameTimingSystem.CurTime;

        var entityQuery = EntityQueryEnumerator<ERPArousalComponent>();

        while (entityQuery.MoveNext(out var uid, out var component)) {
            if (component.NextUpdate > curTime)
                continue;
            component.NextUpdate = curTime + component.UpdateFrequency;
            UpdateEntity((uid,component));
        }
    }

    public virtual void UpdateEntity(Entity<ERPArousalComponent> ent) {
        //_sawmill.Debug($"Updating {Name(ent.Owner)}");
        if (ent.Comp.ArousalBaseline - ent.Comp.CurrentArousal != 0.0f) {
            if (ent.Comp.ArousalBaseline - ent.Comp.CurrentArousal <= FixedPoint2.Epsilon) {
                ent.Comp.CurrentArousal = ent.Comp.ArousalBaseline;
            } else {
                var arousalAddition = (ent.Comp.ArousalBaseline - ent.Comp.CurrentArousal) * _arousalMultiplier;
                ent.Comp.CurrentArousal += arousalAddition <= FixedPoint2.Epsilon ? FixedPoint2.Epsilon : arousalAddition;
            }
        }
        Dirty(ent);
    }
};
