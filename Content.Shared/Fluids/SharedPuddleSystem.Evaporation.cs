using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    private const string FluorosulfuricAcid = "FluorosulfuricAcid"; // Frontier
    private const string Vomit = "Vomit"; // Frontier
    private const string InsectBlood = "InsectBlood"; // Frontier
    private const string AmmoniaBlood = "AmmoniaBlood"; // Frontier
    private const string ZombieBlood = "ZombieBlood"; // Frontier
    private const string Blood = "Blood"; // Frontier
    private const string Slime = "Slime"; // Frontier
    private const string CopperBlood = "CopperBlood"; // Frontier
    private const string Sap = "Sap"; // Frontier
    private const string JuiceTomato = "JuiceTomato"; // Frontier

    public static readonly string[] EvaporationReagents = [Water, Vomit, InsectBlood, AmmoniaBlood, ZombieBlood, Blood, Slime, CopperBlood, FluorosulfuricAcid, Sap, JuiceTomato]; // Frontier

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
