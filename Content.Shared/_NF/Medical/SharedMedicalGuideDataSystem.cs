using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Kitchen;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Client.Chemistry.EntitySystems;

// A clone of the FoodGuideDataSystem. Thank you to Mnemotechnician for the original implementation.
// Redundancy.
public abstract class SharedMedicalGuideDataSystem : EntitySystem
{
    public List<MedicalGuideEntry> Registry = new();
}

[Serializable, NetSerializable]
public sealed class MedicalGuideRegistryChangedEvent : EntityEventArgs
{
    [DataField]
    public List<MedicalGuideEntry> Changeset;

    public MedicalGuideRegistryChangedEvent(List<MedicalGuideEntry> changeset)
    {
        Changeset = changeset;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial struct MedicalGuideEntry
{
    [DataField]
    public EntProtoId Result;

    [DataField]
    public string Identifier; // Used for sorting

    [DataField]
    public MedicalRecipeData[] Recipes;

    [DataField]
    public ReagentQuantity[] Composition;

    [DataField]
    public DamageSpecifier? Healing;

    public MedicalGuideEntry(EntProtoId result, string identifier, MedicalRecipeData[] recipes, ReagentQuantity[] composition, DamageSpecifier? healing)
    {
        Result = result;
        Identifier = identifier;
        Recipes = recipes;
        Composition = composition;
        Healing = healing;
    }
}

[Serializable, NetSerializable]
public sealed partial class MedicalRecipeData
{
    [DataField]
    public ProtoId<FoodRecipePrototype> Recipe;

    [DataField]
    public EntProtoId Result;

    [DataField]
    private int _outputCount;
    public int OutputCount => _outputCount;

    /// <summary>
    ///     A string used to distinguish different sources. Typically the name of the related entity.
    /// </summary>
    public string Identitier;

    public MedicalRecipeData(FoodRecipePrototype proto)
    {
        Identitier = proto.Name;
        Recipe = proto.ID;
        Result = proto.Result;
        _outputCount = proto.ResultCount;
    }
}
