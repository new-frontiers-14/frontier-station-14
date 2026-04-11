using Content.Shared._CitadelStation.ERP.Interaction.Systems;
using Content.Shared._CitadelStation.ERP.Interaction.Components;
using Content.Shared._CitadelStation.ERP.Interaction.UI.Messages;
using Content.Server.EntityEffects;
using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;

namespace Content.Server._CitadelStation.ERP.Interaction.Systems;


public class ArousalSystem : SharedArousalSystem
{

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

    public override void Initialize()
    {
        SubscribeLocalEvent<ERPArousalComponent, ERPActionUserInterfaceMessage>(OnERPInteraction);
    }

    public override void UpdateEntity(Entity<ERPArousalComponent> ent)
    {
        base.UpdateEntity(ent);

        var curTime = _gameTimingSystem.CurTime;

        if (ent.Comp.NextMoan > curTime)

            if (ent.Comp.CurrentArousal > ent.Comp.MoanThreshold && ent.Comp.NextMoan > curTime)
            {
                if (_robustRandom.NextFloat() < 0.3f)
                {
                    _jitter.DoJitter(ent, TimeSpan.FromSeconds(3), true);
                    _stamina.TakeStaminaDamage(ent, 20.0f);
                    ent.Comp.CurrentArousal -= 0.2;
                    _chatSystem.TryEmoteWithChat(ent, "Moan");

                    ent.Comp.NextMoan += ent.Comp.MoanCoolDown;
                }
            }

        Dirty(ent);
    }
    private void OnERPInteraction(Entity<ERPArousalComponent> entity, ref ERPActionUserInterfaceMessage args)
    {

        // We dont want to deal with entities on which we dont have conainers
        //if (!EntityManager.TryGetComponent<ContainerManagerComponent>(entity.Owner, out var containers))
        //    return;

        // We dont want to deal with entities that dont have jumpsuit slot.
        //if (!containers.Containers.ContainsKey("jumpsuit"))
        //    return;
        Logger.Debug($"{args.Interactor} - {args.Bodypart} - {args.Mode}");
        // Interaction with groin
        if (args.Actor != entity.Owner) {
            if (args.Interactor == ERPInteractionMenuInteractorOptions.Groin) {
                // Groin-To-Mouth/Head interaction
                if (args.Bodypart == ERPInteractionMenuBodyParts.Head) {
                    // Rough insertion: deepthroat
                    if (args.Mode == ERPInteractionMenuInteractorModes.Rough) {
                        _ = _chatSystem.TryEmoteWithChat(args.Actor, "PenisInsertionOral", forceEmote: true);
                        if (_robustRandom.NextFloat() < 0.3f) {
                            _chatSystem.TryEmoteWithChat(entity.Owner,"FemaleMediumDeepthroatEmote", forceEmote: true);
                        }
                    // Gentle interaction: oral
                    } else if (args.Mode == ERPInteractionMenuInteractorModes.Gentle) {

                    }
                }
            }
        }
    }
};
