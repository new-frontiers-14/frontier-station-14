using Content.Shared._CitadelStation.ERP.Interaction.Systems;
using Content.Shared._CitadelStation.ERP.Interaction.Components;
using Content.Server.EntityEffects;
using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage.Systems;

namespace Content.Server._CitadelStation.ERP.Interaction.Systems;


public class ArousalSystem : SharedArousalSystem {

    [Dependency] private readonly EntityEffectSystem _entityEffect = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTimingSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    //private ISawmill _sawmill = default!;

    /*public override void Initialize() {
        //base.Initialize();
        _sawmill = Logger.GetSawmill("ArousalServer");
        _sawmill.Debug("Engaged");
    }*/

    public override void UpdateEntity(Entity<ERPArousalComponent> ent) {
        base.UpdateEntity(ent);

        if (ent.Comp.CurrentArousal > ent.Comp.MoanThreshold)
        {
            if (_robustRandom.NextFloat() < 0.3f)
            {
                _jitter.DoJitter(ent, TimeSpan.FromSeconds(3), true);
                _stamina.TakeStaminaDamage(ent, 20.0f);
                ent.Comp.CurrentArousal -= 0.2;
                _chatSystem.TryEmoteWithChat(ent, "Moan");
            }
        }
        Dirty(ent);
    }

};
