using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes; // Frontier

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    private static readonly ProtoId<ReagentPrototype> FluorosulfuricAcid = "FluorosulfuricAcid"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Vomit = "Vomit"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Holywater = "Holywater"; // Frontier
    private static readonly ProtoId<ReagentPrototype> InsectBlood = "InsectBlood"; // Frontier
    private static readonly ProtoId<ReagentPrototype> AmmoniaBlood = "AmmoniaBlood"; // Frontier
    private static readonly ProtoId<ReagentPrototype> ZombieBlood = "ZombieBlood"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Blood = "Blood"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Slime = "Slime"; // Frontier
    private static readonly ProtoId<ReagentPrototype> CopperBlood = "CopperBlood"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Sap = "Sap"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Syrup = "Syrup"; // Frontier
    private static readonly ProtoId<ReagentPrototype> JuiceTomato = "JuiceTomato"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Fiber = "Fiber"; // Frontier
    private static readonly ProtoId<ReagentPrototype> Nothing = "Nothing"; // Frontier
    private static readonly ProtoId<ReagentPrototype> GoblinBlood = "GoblinBlood"; // Frontier

    // Frontier: NOTE: if updating this list, keep up to date with AbsorbentSystem.EvaporationReagents
    public static readonly string[] EvaporationReagents = [Water, Vomit, Holywater, InsectBlood, AmmoniaBlood, ZombieBlood, Blood, Slime, CopperBlood, FluorosulfuricAcid, Sap, Syrup, JuiceTomato, Fiber, Nothing, GoblinBlood]; // Frontier

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
