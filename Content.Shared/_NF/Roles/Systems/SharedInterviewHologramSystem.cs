using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Events;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._NF.Roles.Systems;

public abstract partial class SharedInterviewHologramSystem : EntitySystem
{
    [Dependency] protected SharedIdCardSystem IdCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<InterviewHologramComponent, SetCaptainApprovedEvent>(OnSetCaptainApproved);
        SubscribeLocalEvent<InterviewHologramComponent, ToggleApplicantApprovalEvent>(OnToggleApplicantApproval);
    }

    private void OnAlternativeVerb(Entity<InterviewHologramComponent> ent, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract || ev.Hands == null || ev.User == ev.Target)
            return;

        if (IsCaptain(ev.User, ent))
        {
            bool accepted = ent.Comp.CaptainApproved;
            EntityUid captain = ev.User;
            ev.Verbs.Add(new AlternativeVerb()
            {
                Act = () => RaiseLocalEvent(ent, new SetCaptainApprovedEvent(captain, !accepted)),
                Text = Loc.GetString(accepted ? "interview-hologram-rescind" : "interview-hologram-approve"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png"))
            });
            ev.Verbs.Add(new AlternativeVerb()
            {
                Act = () => RaiseLocalEvent(ent, new DismissInterviewEvent(captain)),
                Text = Loc.GetString("interview-hologram-dismiss"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
                Priority = -1
            });
        }
    }

    private void OnSetCaptainApproved(Entity<InterviewHologramComponent> ent, ref SetCaptainApprovedEvent ev)
    {
        if (IsCaptain(ev.Captain, ent))
        {
            ent.Comp.CaptainApproved = ev.Approved;
            Dirty(ent);
            HandleApprovalChanged(ent);
        }
    }

    /// <summary>
    /// Checks if a given entity is the captain of the ship the target entity is on.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <param name="target">The target entity that's on the ship in question.</param>
    protected bool IsCaptain(EntityUid uid, EntityUid target)
    {
        return IdCardSystem.TryFindIdCard(uid, out var idCard)
            && TryComp(idCard, out ShuttleDeedComponent? shuttleDeed)
            && TryComp(target, out TransformComponent? targetXform)
            && shuttleDeed.ShuttleUid == targetXform.GridUid;
    }

    private void OnToggleApplicantApproval(Entity<InterviewHologramComponent> ent, ref ToggleApplicantApprovalEvent ev)
    {
        ent.Comp.ApplicantApproved = !ent.Comp.ApplicantApproved;
        Dirty(ent);
        ev.Toggle = true;
        HandleApprovalChanged(ent);
    }

    abstract protected void HandleApprovalChanged(Entity<InterviewHologramComponent> ent);
}
