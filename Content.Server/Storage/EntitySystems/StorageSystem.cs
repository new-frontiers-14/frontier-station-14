using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Explosion;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageComponent, GetVerbsEvent<ActivationVerb>>(AddUiVerb);
        Subs.BuiEvents<StorageComponent>(StorageComponent.StorageUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
        });
        SubscribeLocalEvent<StorageComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenBackpack, InputCmdHandler.FromDelegate(HandleOpenBackpack))
            .Bind(ContentKeyFunctions.OpenBelt, InputCmdHandler.FromDelegate(HandleOpenBelt))
            .Register<StorageSystem>();
    }

    private void AddUiVerb(EntityUid uid, StorageComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        var silent = false;
        if (!args.CanAccess || !args.CanInteract || TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
        {
            // we allow admins to open the storage anyways
            if (!_admin.HasAdminFlag(args.User, AdminFlags.Admin))
                return;

            silent = true;
        }

        silent |= HasComp<GhostComponent>(args.User);

        // Get the session for the user
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        // Does this player currently have the storage UI open?
        var uiOpen = _uiSystem.SessionHasOpenUi(uid, StorageComponent.StorageUiKey.Key, actor.PlayerSession);

        ActivationVerb verb = new()
        {
            Act = () =>
            {
                if (uiOpen)
                {
                    _uiSystem.TryClose(uid, StorageComponent.StorageUiKey.Key, actor.PlayerSession);
                }
                else
                {
                    OpenStorageUI(uid, args.User, component, silent);
                }
            }
        };
        if (uiOpen)
        {
            verb.Text = Loc.GetString("verb-common-close-ui");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            verb.Text = Loc.GetString("verb-common-open-ui");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        args.Verbs.Add(verb);
    }

    private void OnBoundUIClosed(EntityUid uid, StorageComponent storageComp, BoundUIClosedEvent args)
    {
        if (TryComp<ActorComponent>(args.Session.AttachedEntity, out var actor) && actor?.PlayerSession != null)
            CloseNestedInterfaces(uid, actor.PlayerSession, storageComp);

        // If UI is closed for everyone
        if (!_uiSystem.IsUiOpen(uid, args.UiKey))
        {
            storageComp.IsUiOpen = false;
            UpdateAppearance((uid, storageComp, null));

            if (storageComp.StorageCloseSound is not null)
                Audio.PlayEntity(storageComp.StorageCloseSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, storageComp.StorageCloseSound.Params);
        }
    }

    private void OnExploded(Entity<StorageComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }

    /// <summary>
    ///     Opens the storage UI for an entity
    /// </summary>
    /// <param name="entity">The entity to open the UI for</param>
    public override void OpenStorageUI(EntityUid uid, EntityUid entity, StorageComponent? storageComp = null, bool silent = false)
    {
        if (!Resolve(uid, ref storageComp, false) || !TryComp(entity, out ActorComponent? player))
            return;

        // prevent spamming bag open / honkerton honk sound
        silent |= TryComp<UseDelayComponent>(uid, out var useDelay) && _useDelay.IsDelayed((uid, useDelay));
        if (!silent)
        {
            if (!storageComp.IsUiOpen)
                _audio.PlayPvs(storageComp.StorageOpenSound, uid);
            if (useDelay != null)
                _useDelay.TryResetDelay((uid, useDelay));
        }

        Log.Debug($"Storage (UID {uid}) \"used\" by player session (UID {player.PlayerSession.AttachedEntity}).");

        var bui = _uiSystem.GetUiOrNull(uid, StorageComponent.StorageUiKey.Key);
        if (bui == null)
            return;
        _uiSystem.OpenUi(bui, player.PlayerSession);
        _uiSystem.SendUiMessage(bui, new StorageModifyWindowMessage());
    }

    /// <inheritdoc />
    public override void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates, EntityCoordinates finalCoordinates,
        Angle initialRotation, EntityUid? user = null)
    {
        var filter = Filter.Pvs(uid).RemoveWhereAttachedEntity(e => e == user);
        RaiseNetworkEvent(new PickupAnimationEvent(GetNetEntity(uid), GetNetCoordinates(initialCoordinates), GetNetCoordinates(finalCoordinates), initialRotation), filter);
    }

    /// <summary>
    ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
    /// </summary>
    /// <param name="session"></param>
    public void CloseNestedInterfaces(EntityUid uid, ICommonSession session, StorageComponent? storageComp = null)
    {
        if (!Resolve(uid, ref storageComp))
            return;

        // for each containing thing
        // if it has a storage comp
        // ensure unsubscribe from session
        // if it has a ui component
        // close ui
        foreach (var entity in storageComp.Container.ContainedEntities)
        {
            if (!TryComp(entity, out UserInterfaceComponent? ui))
                continue;

            foreach (var bui in ui.Interfaces.Values)
            {
                _uiSystem.TryClose(entity, bui.UiKey, session, ui);
            }
        }
    }

    private void HandleOpenBackpack(ICommonSession? session)
    {
        HandleOpenSlotUI(session, "back");
    }

    private void HandleOpenBelt(ICommonSession? session)
    {
        HandleOpenSlotUI(session, "belt");
    }

    private void HandleOpenSlotUI(ICommonSession? session, string slot)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not {Valid: true} playerEnt || !Exists(playerEnt))
            return;

        if (!_inventory.TryGetSlotEntity(playerEnt, slot, out var storageEnt))
            return;

        if (!ActionBlocker.CanInteract(playerEnt, storageEnt))
            return;

        OpenStorageUI(storageEnt.Value, playerEnt);
    }
}
