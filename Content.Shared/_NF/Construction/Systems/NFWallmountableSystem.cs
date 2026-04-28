using Content.Shared._NF.Construction.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Construction.Systems;

/// <summary>
/// A system to spawn entities on the wall when interacting with an empty space of wall.
/// </summary>
public sealed partial class NFWallmountableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<TagPrototype> DiagonalTag = "Diagonal";
    private const float IntersectionRange = 0.35f; // The range to look for wallmounts around the center of a wall.

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFWallmountableComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<NFWallmountableComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NFWallmountableComponent, NFWallmountDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// GetVerbs handler - add a verb to mount the entity onto the wall.
    /// </summary>
    private void OnGetVerbs(Entity<NFWallmountableComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanComplexInteract || !args.CanAccess || args.Using == null)
            return;

        // Not a wall, don't show verb.
        if (!IsMountableWall(args.Target))
            return;

        bool canMount = NoWallmountsAtEntity(args.Target);

        var user = args.User;
        var target = args.Target;

        var mountString = Loc.GetString("nf-wallmountable-component-verb-mount");

        args.Verbs.Add(new UtilityVerb
        {
            IconEntity = GetNetEntity(ent),
            Text = mountString,
            Disabled = !canMount,
            Message = canMount ? mountString : Loc.GetString("nf-wallmountable-component-verb-cant-mount"),
            Act = () => TryMount(ent, user, target)
        });
    }

    /// <summary>
    /// Interaction handler - try to spawn the wallmount entity if we can, notify the user via popup if we can't.
    /// </summary>
    private void OnAfterInteract(Entity<NFWallmountableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        // User can't access the wall, leave this unhandled.
        if (!UserCanMountOnTarget(args.User, args.Target.Value))
            return;

        if (!IsMountableWall(args.Target.Value)
            || !TryMount(ent, args.User, args.Target.Value, checkAccess: false))
        {
            _popup.PopupPredicted(Loc.GetString("nf-wallmountable-component-verb-cant-mount"), args.User, args.User);
        }

        args.Handled = true;
    }

    /// <summary>
    /// DoAfter handler - spawns the actual item on the wall and deletes the Wallmountable entity.
    /// </summary>
    private void OnDoAfter(Entity<NFWallmountableComponent> ent, ref NFWallmountDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        // Sanity check.
        if (!UserCanMountOnTarget(args.User, args.Target.Value)
            || !IsMountableWall(args.Target.Value)
            || !NoWallmountsAtEntity(args.Target.Value))
            return;

        if (_net.IsClient)
            return;

        // Spawn our wallmount entity.
        var targetXform = Transform(args.Target.Value);
        var spawn = SpawnAtPosition(ent.Comp.Spawn, targetXform.Coordinates);

        var angle = Angle.Zero;
        if (ent.Comp.RotateToUser)
        {
            // Get world position from target to user (facing them when placed)
            var destWorldRotation = (_transform.GetWorldPosition(args.User) - _transform.GetWorldPosition(targetXform)).ToWorldAngle();

            var parentAngle = _transform.GetWorldRotation(targetXform);

            // Don't trust map savers.
            if (!targetXform.NoLocalRotation)
                parentAngle -= targetXform.LocalRotation;

            angle = (destWorldRotation - parentAngle).GetCardinalDir().ToAngle();
        }

        _transform.SetLocalRotationNoLerp(spawn, angle);

        QueueDel(ent);
    }

    /// <summary>
    /// Tries to start a doafter to mount the given entity on the target.
    /// </summary>
    /// <param name="ent">The entity to mount on the wall.</param>
    /// <param name="user">The user mounting the entity.</param>
    /// <param name="target">The wall to mount the entity on.</param>
    /// <param name="checkAccess">Whether or not to check that the user has access to the wall.</param>
    /// <returns>Whether or not a doafter was started to mount the item on the wall.</returns>
    private bool TryMount(Entity<NFWallmountableComponent> ent, EntityUid user, EntityUid target, bool checkAccess = true)
    {
        var time = ent.Comp.DoAfterTime;

        if (checkAccess)
        {
            if (!UserCanMountOnTarget(user, target)
                || !IsMountableWall(target))
            {
                return false;
            }
        }

        if (!NoWallmountsAtEntity(target))
            return false;

        var ev = new NFWallmountDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, user, time, ev, ent, target: target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            NeedHand = ent.Owner != user,
            DuplicateCondition = DuplicateConditions.SameTool,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    /// <summary>
    /// Returns true if the user is capable and within range of the target.
    /// </summary>
    private bool UserCanMountOnTarget(EntityUid user, EntityUid target)
    {
        return _actionBlocker.CanComplexInteract(user)
            && _interaction.IsAccessible(user, target)
            && _interaction.InRangeUnobstructed(user, target);
    }

    /// <summary>
    /// Returns true if the given entity is a mountable wall.
    /// </summary>
    private bool IsMountableWall(EntityUid target)
    {
        return TryComp<TagComponent>(target, out var tags)
            && _tag.HasTag(tags, WallTag)
            && !_tag.HasTag(tags, DiagonalTag);
    }

    /// <summary>
    /// Checks and returns whether or not an entity can be mounted on the target.
    /// User access must be checked separately from this.
    /// </summary>
    private bool NoWallmountsAtEntity(EntityUid target)
    {
        var targetXform = Transform(target);

        // Check for wallmount items on the tile.
        // Using coordinates vs. entity to avoid fixture lookup, which causes issues with larger entities on surrounding tiles (e.g. windows, firelocks)
        var mapPos = _transform.GetMapCoordinates(targetXform);
        foreach (var entity in _lookup.GetEntitiesInRange(mapPos, IntersectionRange, LookupFlags.StaticSundries))
        {
            if (HasComp<WallMountComponent>(entity))
                return false;
        }
        return true;
    }
}
