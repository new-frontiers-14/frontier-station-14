using Content.Server.Administration.Logs;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Content.Shared.Audio;
using Content.Server.Body.Components;
using Content.Shared.ArachnidChaos;
using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Player;
using Content.Shared.Mobs.Systems;
using Content.Server.Body.Systems;
using Content.Server.Nutrition.Components;
using Content.Shared.Database;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.ArachnidChaos
{
    public sealed class ArachnidChaosSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArachnidChaosComponent, GetVerbsEvent<InnateVerb>>(AddBiteVerb);
            SubscribeLocalEvent<ArachnidChaosComponent, ArachnidChaosDoAfterEvent>(OnDoAfter);
        }
        private void AddBiteVerb(EntityUid uid, ArachnidChaosComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!_mobStateSystem.IsAlive(args.User))
                return;

            if (args.User == args.Target)
                return;

            if (!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    if (!IsActionValid(args.User, args.Target))
                        return;

                    _popupSystem.PopupEntity(Loc.GetString("spider-biting", ("UsernameName", args.User), ("targetName", args.Target)), args.User);
                    _popupSystem.PopupEntity(Loc.GetString("spider-biting"), args.User, args.User);

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, 3f, new ArachnidChaosDoAfterEvent(), uid, target: args.Target, used: uid)
                    {
                        BreakOnMove = true,
                        BreakOnDamage = true,
                        BlockDuplicate = true
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                },
                Icon = new SpriteSpecifier.Rsi(new ("Nyanotrasen/Icons/verbiconfangs.rsi"), "icon"),
                Text = Loc.GetString("action-name-spider-bite"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
        private void OnDoAfter(EntityUid uid, ArachnidChaosComponent comp, DoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            if (!IsActionValid(args.Args.User, args.Args.Target.Value))
                return;

            if (!TryComp<HungerComponent>(args.Args.User, out var hunger))
                return;

            if (!TryComp<BloodstreamComponent>(args.Args.Target.Value, out var bloodstream))
                return;

            _bloodstreamSystem.TryModifyBloodLevel(args.Args.Target.Value, -5, bloodstream);
            _audio.PlayPvs("/Audio/Items/drink.ogg", args.Args.User, AudioHelpers.WithVariation(0.15f));
            _hunger.ModifyHunger(args.Args.User, 5, hunger);

            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.Args.User):actor} drank blood from {ToPrettyString(args.Args.Target.Value):actor}");

            args.Repeat = true;
        }

        private bool IsActionValid(EntityUid user, EntityUid target)
        {
            if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
                return false;

            if (bloodstream.BloodReagent == "Blood")
            {
                if (_bloodstreamSystem.GetBloodLevelPercentage(target, bloodstream) <= 0.0f)
                {
                    _popupSystem.PopupEntity(Loc.GetString("no-blood-warning"), user, user, Shared.Popups.PopupType.SmallCaution);
                    return false;
                }
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("no-good-blood"), user, user, Shared.Popups.PopupType.SmallCaution);
                return false;
            }

            if (!TryComp<HungerComponent>(user, out var hunger))
                return false;

            if (hunger.CurrentThreshold == Shared.Nutrition.Components.HungerThreshold.Overfed)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), user, user, Shared.Popups.PopupType.SmallCaution);
                return false;
            }

            if (_inventorySystem.TryGetSlotEntity(user, "mask", out var maskUid) &&
            EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
            blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), user, user, Shared.Popups.PopupType.SmallCaution);
                return false;
            }

            return true;
        }
    }
}
