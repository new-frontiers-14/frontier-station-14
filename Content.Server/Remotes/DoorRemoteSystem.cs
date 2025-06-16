using Content.Server.Administration.Logs;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._NF.GridAccess; // Frontier
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;

namespace Content.Shared.Remotes
{
    public sealed class DoorRemoteSystem : SharedDoorRemoteSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly GridAccessSystem _gridAccessSystem = default!; // Frontier

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        }

        private void OnBeforeInteract(Entity<DoorRemoteComponent> entity, ref BeforeRangedInteractEvent args)
        {
            bool isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
                // Only able to control doors if they are within your vision and within your max range.
                // Not affected by mobs or machines anymore.
                || !_examine.InRangeUnOccluded(args.User,
                    args.Target.Value,
                    SharedInteractionSystem.MaxRaycastRange,
                    null))

            {
                return;
            }

            args.Handled = true;

            // Frontier: Grid access restriction
            if (TryComp<GridAccessComponent>(entity.Owner, out var gridAccessComponent))
            {
                string? popupMessage = null;
                if (!TryComp(args.Target.Value, out TransformComponent? xform)
                    || xform.GridUid == null
                    || !GridAccessSystem.IsAuthorized(xform.GridUid, gridAccessComponent, out popupMessage))
                {
                    if (popupMessage != null)
                    {
                        Popup.PopupEntity(Loc.GetString("door-remote-" + popupMessage), args.Used, args.User);
                    }
                    return;
                }

                if (!doorComp.RemoteCompatible)
                {
                    Popup.PopupEntity(Loc.GetString("door-remote-use-blocked"), args.Used, args.User);
                    return;
                }
            }
            // End Frontier: Grid access restriction

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                Popup.PopupEntity(Loc.GetString("door-remote-no-power"), args.User, args.User);
                return;
            }

            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent)
                && !_doorSystem.HasAccess(args.Target.Value, args.Used, doorComp, accessComponent))
            {
                if (isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, args.User);
                Popup.PopupEntity(Loc.GetString("door-remote-denied"), args.User, args.User);
                return;
            }

            switch (entity.Comp.Mode)
            {
                case OperatingMode.OpenClose:
                    if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, args.Used))
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                    break;
                case OperatingMode.ToggleBolts:
                    if (TryComp<DoorBoltComponent>(args.Target, out var boltsComp))
                    {
                        if (!boltsComp.BoltWireCut)
                        {
                            _doorSystem.SetBoltsDown((args.Target.Value, boltsComp), !boltsComp.BoltsDown, args.Used);
                            _adminLogger.Add(LogType.Action,
                                LogImpact.Medium,
                                $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                        }
                    }

                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    if (airlockComp != null)
                    {
                        _airlock.SetEmergencyAccess((args.Target.Value, airlockComp), !airlockComp.EmergencyAccess);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to set emergency access {(airlockComp.EmergencyAccess ? "on" : "off")}");
                    }

                    break;
                default:
                    throw new InvalidOperationException(
                        $"{nameof(DoorRemoteComponent)} had invalid mode {entity.Comp.Mode}");
            }
        }
    }
}
