using Content.Shared.Materials;

namespace Content.Client.Materials;

/// <inheritdoc/>
public sealed class MaterialReclaimerSystem : SharedMaterialReclaimerSystem
{
    // Frontier: shut the reclaimer up when it's done if we missed something.
    public override bool TryFinishProcessItem(EntityUid uid, MaterialReclaimerComponent? component = null, ActiveMaterialReclaimerComponent? active = null)
    {
        // We only need the reclaimer component for this.
        if (!Resolve(uid, ref component, false))
            return false;

        // Stop the stream if it exists.
        if (component.CutOffSound && component.Stream != null)
            _audio.Stop(component.Stream);

        return base.TryFinishProcessItem(uid, component, active);
    }
    // End Frontier
}
