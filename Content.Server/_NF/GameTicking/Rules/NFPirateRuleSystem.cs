using Content.Server._NF.GameTicking.Rules.Components;
using Content.Server._NF.Roles;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.GameTicking.Rules;

public sealed class NFPirateRuleSystem : GameRuleSystem<NFPirateRuleComponent>
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultNFPirateRule = "NFPirate";

    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFPirateRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<NFPirateRoleComponent, GetBriefingEvent>(OnGetBriefing);

        SubscribeLocalEvent<NFPirateComponent, ComponentInit>(OnComponentInit);
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
            ? Loc.GetString("pirate-role-greeting-human")
            : Loc.GetString("pirate-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("pirate-role-greeting-equipment") + "\n";

        return briefing;
    }

    private void OnComponentInit(EntityUid uid, NFPirateComponent component, ref ComponentInit args)
    {
        if (!HasComp<MindContainerComponent>(uid) || !TryComp<ActorComponent>(uid, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        _antag.ForceMakeAntag<NFPirateRuleComponent>(targetPlayer, DefaultNFPirateRule); // Frontier
    }
}
