using Content.Server._NF.GameTicking.Rules.Components;
using Content.Server._NF.Pirate.Components;
using Content.Server.Antag;
using Content.Shared.Mind.Components;

namespace Content.Server._NF.Pirate.Systems;

// Rule-independent system that ensures if auto-pirates get added, the rules get set up properly.
public sealed class AutoPirateSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoPirateComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AutoPirateCaptainComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, Component _, MindAddedMessage args)
    {
        _antag.ForceMakeAntag<NFPirateRuleComponent>(args.Mind.Comp.Session, "NFPirate");
    }
}
