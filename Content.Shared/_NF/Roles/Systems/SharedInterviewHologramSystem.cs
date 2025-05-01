using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Events;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Systems;

namespace Content.Server._NF.Roles.Systems;

public abstract partial class SharedInterviewHologramSystem : EntitySystem
{
    [Dependency] protected SharedIdCardSystem IdCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, SetCaptainApprovedEvent>(OnSetCaptainApproved);
        SubscribeLocalEvent<InterviewHologramComponent, ToggleApplicantApprovalEvent>(OnToggleApplicantApproval);
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
        HandleApprovalChanged(ent);
        ev.Toggle = true;
        ev.Handled = true;
    }

    /// <summary>
    /// An abstract approval handler, expected to be defined server- and client-side.
    /// </summary>
    /// <param name="ent">The entity whose approval state has changed.</param>
    abstract protected void HandleApprovalChanged(Entity<InterviewHologramComponent> ent);
}
