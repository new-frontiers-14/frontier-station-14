using Content.Server.Materials;
using Content.Shared.Materials;

namespace Content.Server._NF.Power.Generator;

public sealed class FuelGradeAdapterSystem : EntitySystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuelGradeAdapterComponent, MaterialEntityInsertedEvent>(OnMaterialEntityInserted);
    }

    public void OnMaterialEntityInserted(Entity<FuelGradeAdapterComponent> entity, ref MaterialEntityInsertedEvent args)
    {
        // Convert all of the input material we can in the material storage into output material
        if (!TryComp<MaterialStorageComponent>(entity.Owner, out var materialStorage))
            return;

        foreach (var conversion in entity.Comp.Conversions)
        {
            var inputAmount = _materialStorage.GetMaterialAmount(entity.Owner, conversion.Input, materialStorage);
            if (inputAmount > 0)
            {
                _materialStorage.TryChangeMaterialAmount(entity.Owner, conversion.Input, -inputAmount, materialStorage, dirty: false);
                _materialStorage.TryChangeMaterialAmount(entity.Owner, conversion.Output, (int)(inputAmount * conversion.Rate), materialStorage, dirty: true);
            }
        }
    }
}

