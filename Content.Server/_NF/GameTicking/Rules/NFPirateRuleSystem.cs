using Content.Server._NF.GameTicking.Rules.Components;
using Content.Server._NF.Pirate.Components;
using Content.Server._NF.Roles.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.Humanoid;
using Content.Shared.NPC.Systems;

namespace Content.Server._NF.GameTicking.Rules;

public sealed class NFPirateRuleSystem : GameRuleSystem<NFPirateRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFPirateRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
        SubscribeLocalEvent<NFPirateRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon pirate activation
    private void AfterAntagSelected(Entity<NFPirateRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);

        if (TryComp(ent, out AutoPirateComponent? pirate) && !pirate.ApplyFaction)
            return;

        if (TryComp(ent, out AutoPirateCaptainComponent? captain) && !captain.ApplyFaction)
            return;

        _npcFaction.RemoveFaction(ent, mindId.Comp.NanoTrasenFaction, false);
        _npcFaction.AddFaction(ent, mindId.Comp.PirateFaction);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<NFPirateRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid uid)
    {
        string ret;
        // This is hacky.
        if (HasComp<AutoPirateCaptainComponent>(uid))
            ret = Loc.GetString("nf-piratecaptain-role-greeting");
        else
            ret = Loc.GetString("nf-pirate-role-greeting");

        if (HasComp<HumanoidAppearanceComponent>(uid))
            ret += "\n\n" + Loc.GetString("nf-pirate-role-greeting-equipment") + "\n";
        return ret;
    }
}
