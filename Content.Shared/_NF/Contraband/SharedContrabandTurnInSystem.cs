using Content.Shared.Contraband;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Contraband;

[NetSerializable, Serializable]
public enum ContrabandPalletConsoleUiKey : byte
{
    Contraband
}

public abstract class SharedContrabandTurnInSystem : EntitySystem
{
    public void ClearContrabandValue(EntityUid item)
    {
        // Clear contraband value for printed items
        if (TryComp<ContrabandComponent>(item, out var contraband))
        {
            foreach (var valueKey in contraband.TurnInValues.Keys)
            {
                contraband.TurnInValues[valueKey] = 0;
            }
        }

        // Recurse into contained entities
        if (TryComp<ContainerManagerComponent>(item, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    ClearContrabandValue(ent);
                }
            }
        }
    }
}
