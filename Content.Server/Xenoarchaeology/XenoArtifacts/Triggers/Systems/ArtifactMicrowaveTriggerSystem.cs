using Content.Server.Kitchen.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactMicrowaveTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactMicrowaveTriggerComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(EntityUid uid, ArtifactMicrowaveTriggerComponent component, BeingMicrowavedEvent args)
    {
        // Frontier: microwave trigger requires radiation, check if this can irradiate
        if (!args.BeingIrradiated)
            return;
        // End Frontier

        _artifact.TryActivateArtifact(uid);
    }
}
