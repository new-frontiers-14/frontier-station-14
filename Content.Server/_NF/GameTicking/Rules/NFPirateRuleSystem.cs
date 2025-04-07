using Content.Server._NF.GameTicking.Rules.Components;
using Content.Server._NF.Roles;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.Humanoid;

namespace Content.Server._NF.GameTicking.Rules;

public sealed class NFPirateRuleSystem : GameRuleSystem<NFPirateRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

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
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<NFPirateRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isHuman
            ? Loc.GetString("nf-pirate-role-greeting-human")
            : Loc.GetString("pirate-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("pirate-role-greeting-equipment") + "\n";

        return briefing;
    }
}
