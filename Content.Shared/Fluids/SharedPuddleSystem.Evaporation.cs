using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes; // Frontier

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    private static readonly ProtoId<ReagentPrototype> Holywater = "Holywater"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Ice = "Ice"; // Frontier
    private static readonly ProtoId<ReagentPrototype> SodaWater = "SodaWater"; // Frontier
    private static readonly ProtoId<ReagentPrototype> AntiSepticFluid = "AntiSepticFluid"; // Frontier: evaporates, not usable as mop water

    // Frontier: NOTE: if updating this list, keep up to date with AbsorbentSystem.MopFriendlyReagents
    public static readonly string[] EvaporationReagents = [Water, Holywater, Ice, SodaWater, AntiSepticFluid]; // Frontier

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
