using Content.Server._NF.Pirate.Components;
using Content.Server.Antag;
using Content.Shared.Mind.Components;
using Robust.Server.Player;

namespace Content.Server._NF.Pirate.Systems;

// Rule-independent system that ensures if auto-pirates get added, the rules get set up properly.
public sealed class AutoPirateSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoPirateComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AutoPirateFirstMateComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AutoPirateCaptainComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, Component _, MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;

        _antag.ForceMakeAntag<AutoPirateComponent>(session, "NFPirate");
    }
}
