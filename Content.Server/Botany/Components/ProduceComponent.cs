using Content.Server.Botany.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(BotanySystem))]
public sealed partial class ProduceComponent : Component
{
    [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

    /// <summary>
    ///     Seed data used to create a <see cref="SeedComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField("seed")]
    public SeedData? Seed;

    /// <summary>
    ///     Seed data used to create a <see cref="SeedComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField("seedId", customTypeSerializer: typeof(PrototypeIdSerializer<SeedPrototype>))]
    public string? SeedId;
}
