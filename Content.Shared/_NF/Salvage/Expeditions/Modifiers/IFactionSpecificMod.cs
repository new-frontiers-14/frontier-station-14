using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Salvage.Expeditions.Modifiers;

public interface IFactionSpecificMod : IBiomeSpecificMod
{
    /// <summary>
    /// Whitelist for factions this mod can appear with. If null then any faction is allowed.
    /// </summary>
    List<ProtoId<SalvageFactionPrototype>>? Factions { get; }
}
