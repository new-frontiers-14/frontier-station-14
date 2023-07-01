using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Administration.Logs;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.OfHolding
{
    public sealed class OfHoldingSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OfHoldingComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, OfHoldingComponent component, AfterInteractEvent args)
        {
            if (args.Target == null)
                return;

            if (HasComp<OfHoldingComponent>(args.Target))
            {
                if (component.LastWarnedEntity != args.User)
                {
                    component.LastWarnedEntity = args.User;
                    _popupSystem.PopupEntity(Loc.GetString("of-holding-warn"), uid, args.User);
                    return;
                }
                EntityManager.SpawnEntity("Singularity", Transform(args.User).Coordinates);
                _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.User):player} released a singularity by combining 2 bags of holding.");
            }
        }
    }
}
