using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Interaction.Events;

namespace Content.Server.Corvax.Debug;

public sealed class ArtifactActivatingDebugSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ThrowArtifactComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid entity, ThrowArtifactComponent component, UseInHandEvent e)
    {
        RaiseLocalEvent<ArtifactActivatedEvent>(entity, new());
    }
}
