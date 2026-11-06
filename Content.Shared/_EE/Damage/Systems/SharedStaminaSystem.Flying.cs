using Content.Shared.Damage.Components;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedStaminaSystem : EntitySystem
{
    public void ToggleStaminaDrain(EntityUid target, float drainRate, bool enabled, bool modifiesSpeed, EntityUid? source = null)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina))
            return;

        // If theres no source, we assume its the target that caused the drain.
        var actualSource = source ?? target;

        if (enabled)
        {
            stamina.ActiveDrains[actualSource] = (drainRate, modifiesSpeed);
            EnsureComp<ActiveStaminaComponent>(target);
        }
        else
            stamina.ActiveDrains.Remove(actualSource);

        Dirty(target, stamina);
    }
}