using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared._NF.Shipyard.Events;
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
        SubscribeLocalEvent<InterviewHologramComponent, SetApplicantApprovedEvent>(OnSetApplicantApproved);
    }

    private void OnAlternativeVerb(Entity<InterviewHologramComponent> ent, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract || ev.Hands == null)
            return;

        if (ev.User == ev.Target)
        {
            bool accepted = ent.Comp.ApplicantApproved;
            ev.Verbs.Add(new AlternativeVerb()
            {
                Act = () => RaiseLocalEvent(ent, new SetApplicantApprovedEvent(!accepted)),
                Text = Loc.GetString(accepted ? "interview-hologram-rescind" : "interview-hologram-approve"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
                Priority = 4
            });
        }
        else if (IdCardSystem.TryFindIdCard(ev.User, out var idCard)
                && TryComp(idCard, out ShuttleDeedComponent? shuttleDeed)
                && TryComp(ev.Target, out TransformComponent? targetXform)
                && shuttleDeed.ShuttleUid == targetXform.GridUid)
        {
            bool accepted = ent.Comp.CaptainApproved;
            EntityUid captain = ev.User;
            ev.Verbs.Add(new AlternativeVerb()
            {
                Act = () => RaiseLocalEvent(ent, new SetCaptainApprovedEvent(captain, !accepted)),
                Text = Loc.GetString(accepted ? "interview-hologram-rescind" : "interview-hologram-approve"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
                Priority = 4
            });
        }
    }

    private void OnSetCaptainApproved(Entity<InterviewHologramComponent> ent, ref SetCaptainApprovedEvent ev)
    {
        if (IdCardSystem.TryFindIdCard(ev.Captain, out var idCard)
                && TryComp(idCard, out ShuttleDeedComponent? shuttleDeed)
                && TryComp(ent, out TransformComponent? targetXform)
                && shuttleDeed.ShuttleUid == targetXform.GridUid)
        {
            ent.Comp.CaptainApproved = ev.Approved;
            Dirty(ent);
        }
    }

    private void OnSetApplicantApproved(Entity<InterviewHologramComponent> ent, ref SetApplicantApprovedEvent ev)
    {
        ent.Comp.ApplicantApproved = ev.Approved;
        Dirty(ent);
    }

    abstract protected void HandleApprovalChanged(Entity<InterviewHologramComponent> ent);
}
