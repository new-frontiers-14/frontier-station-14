using Content.Server.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
<<<<<<< HEAD
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
=======
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78
using Content.Shared.Morgue;
using Content.Shared.Morgue.Components;
using Content.Shared.Popups;
<<<<<<< HEAD
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
=======
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78
using Robust.Shared.Player;
using Robust.Server.Player; // Frontier

namespace Content.Server.Morgue;
public sealed class CrematoriumSystem : SharedCrematoriumSystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
<<<<<<< HEAD
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!; // Frontier
    [Dependency] private readonly IPlayerManager _player = default!; // Frontier
=======
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrematoriumComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
    }

<<<<<<< HEAD
    private void OnExamine(EntityUid uid, CrematoriumComponent component, ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        using (args.PushGroup(nameof(CrematoriumComponent)))
        {
            if (_appearance.TryGetData<bool>(uid, CrematoriumVisuals.Burning, out var isBurning, appearance) &&
                isBurning)
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning",
                    ("owner", uid)));
            }

            if (_appearance.TryGetData<bool>(uid, StorageVisuals.HasContents, out var hasContents, appearance) &&
                hasContents)
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
            }
        }
    }

    private void OnAttemptOpen(EntityUid uid, ActiveCrematoriumComponent component, ref StorageOpenAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void AddCremateVerb(EntityUid uid, CrematoriumComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || storage.Open)
            return;

        if (HasComp<ActiveCrematoriumComponent>(uid))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("cremate-verb-get-data-text"),
            // TODO VERB ICON add flame/burn symbol?
            Act = () => TryCremate(uid, component, storage),
            Impact = LogImpact.High // could be a body? or evidence? I dunno.
        };
        args.Verbs.Add(verb);
    }

    public bool Cremate(EntityUid uid, CrematoriumComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return false;

        if (HasComp<ActiveCrematoriumComponent>(uid))
            return false;

        _audio.PlayPvs(component.CremateStartSound, uid);
        _appearance.SetData(uid, CrematoriumVisuals.Burning, true);

        _audio.PlayPvs(component.CrematingSound, uid);

        AddComp<ActiveCrematoriumComponent>(uid);
        return true;
    }

    public bool TryCremate(EntityUid uid, CrematoriumComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return false;

        if (storage.Open || storage.Contents.ContainedEntities.Count < 1)
            return false;

        // Frontier - refuse to accept alive mobs and dead-but-connected players
        var entity = storage.Contents.ContainedEntities[0];
        if (entity is not { Valid: true })
            return false;
        if (TryComp<MobStateComponent>(entity, out var comp) && !_mobState.IsDead(entity, comp))
            return false;
        if (_player.TryGetSessionByEntity(entity, out var session) && session.State.Status == SessionStatus.InGame)
            return false;
        // End Frontier

        return Cremate(uid, component, storage);
    }

    private void FinishCooking(EntityUid uid, CrematoriumComponent component, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return;

        _appearance.SetData(uid, CrematoriumVisuals.Burning, false);
        RemComp<ActiveCrematoriumComponent>(uid);

        if (storage.Contents.ContainedEntities.Count > 0)
        {
            for (var i = storage.Contents.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var item = storage.Contents.ContainedEntities[i];
                _containers.Remove(item, storage.Contents);
                Del(item);
            }
            var ash = Spawn("Ash", Transform(uid).Coordinates);
            _containers.Insert(ash, storage.Contents);
        }

        _entityStorage.OpenStorage(uid, storage);
        _audio.PlayPvs(component.CremateFinishSound, uid);
    }

    private void OnSuicideByEnvironment(EntityUid uid, CrematoriumComponent component, SuicideByEnvironmentEvent args)
=======
    private void OnSuicideByEnvironment(Entity<CrematoriumComponent> ent, ref SuicideByEnvironmentEvent args)
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78
    {
        if (args.Handled)
            return;

        var victim = args.Victim;
        if (HasComp<ActorComponent>(victim) && Mind.TryGetMind(victim, out var mindId, out var mind))
        {
            _ghostSystem.OnGhostAttempt(mindId, false, mind: mind);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                Popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message"), entity);
            }
        }

        Popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message-others",
            ("victim", Identity.Entity(victim, EntityManager))),
            victim,
            Filter.PvsExcept(victim),
            true,
            PopupType.LargeCaution);

        if (EntityStorage.CanInsert(victim, ent.Owner))
        {
            EntityStorage.CloseStorage(ent.Owner);
            Standing.Down(victim, false);
            EntityStorage.Insert(victim, ent.Owner);
        }
        else
        {
            EntityStorage.CloseStorage(ent.Owner);
            Del(victim);
        }
        Cremate(ent.AsNullable());
        args.Handled = true;
    }
}
