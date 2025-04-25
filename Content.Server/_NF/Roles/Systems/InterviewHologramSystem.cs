using Content.Server.GameTicking;
using Content.Shared._NF.Roles.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server._NF.Roles.Systems;

public sealed class InterviewHologramSystem : EntitySystem
{
    [Dependency] private SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, MapInitEvent>(OnHologramMapInit);
        SubscribeLocalEvent<InterviewHologramComponent, MindAddedMessage>(OnHologramMindAdded);
    }

    private void OnHologramMapInit(Entity<InterviewHologramComponent> ent, ref MapInitEvent ev)
    {
        // Apply the current character's appearance from their profile, if it exists.
        if (!TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
            return;

        ApplyAppearanceForSession(ent, session);
    }

    private void OnHologramMindAdded(Entity<InterviewHologramComponent> ent, ref MindAddedMessage ev)
    {
        // Apply the current character's appearance from their profile, if it exists and hasn't already been applied.
        if (ent.Comp.AppearanceApplied
            || !TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
            return;

        ApplyAppearanceForSession(ent, session);
    }

    private void ApplyAppearanceForSession(Entity<InterviewHologramComponent> ent, ICommonSession session)
    {
        var profile = _gameTicker.GetPlayerProfile(session);
        _humanoid.LoadProfile(ent, profile);
        _meta.SetEntityName(ent, profile.Name); // Frontier: profile.Name<name
        ent.Comp.AppearanceApplied = true;
    }
}
