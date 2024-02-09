using Content.Server.Interaction.Components;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (HasComp<MobStateComponent>(uid))
            return;

        TryHug(uid, component, args.User);
    }

    public void TryHug(EntityUid uid, InteractionPopupComponent component, EntityUid user)
    {
        if (uid == user)
            return;

        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return;

        string msg = ""; // Stores the text to be shown in the popup message
        string? sfx = null; // Stores the filepath of the sound to be played

        if (_random.Prob(component.SuccessChance))
        {
            if (component.InteractSuccessString != null)
                msg = Loc.GetString(component.InteractSuccessString, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            if (component.InteractSuccessSound != null)
                sfx = component.InteractSuccessSound.GetSound();

            if (component.InteractSuccessSpawn != null)
                Spawn(component.InteractSuccessSpawn, Transform(uid).MapPosition);
        }
        else
        {
            if (component.InteractFailureString != null)
                msg = Loc.GetString(component.InteractFailureString, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            if (component.InteractFailureSound != null)
                sfx = component.InteractFailureSound.GetSound();

            if (component.InteractFailureSpawn != null)
                Spawn(component.InteractFailureSpawn, Transform(uid).MapPosition);
        }

        if (component.MessagePerceivedByOthers != null)
        {
            string msgOthers = Loc.GetString(component.MessagePerceivedByOthers,
                ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msg, uid, user);
            _popupSystem.PopupEntity(msgOthers, uid, Filter.PvsExcept(user, entityManager: EntityManager), true);
        }
        else
            _popupSystem.PopupEntity(msg, uid, user); //play only for the initiating entity.

        if (sfx is not null) //not all cases will have sound.
        {
            if (component.SoundPerceivedByOthers)
                SoundSystem.Play(sfx, Filter.Pvs(uid), uid); //play for everyone in range
            else
                SoundSystem.Play(sfx, Filter.Entities(user, uid), uid); //play only for the initiating entity and its target.
        }

        component.LastInteractTime = curTime;
    }
}
