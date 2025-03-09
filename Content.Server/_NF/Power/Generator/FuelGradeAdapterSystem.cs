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
        // Convert all of the input material in the material storage into output material
        if (!TryComp<MaterialStorageComponent>(entity.Owner, out var materialStorage))
            return;

        var inputAmount = _materialStorage.GetMaterialAmount(entity.Owner, entity.Comp.InputMaterial, materialStorage);
        if (inputAmount > 0)
        {
            _materialStorage.TryChangeMaterialAmount(entity.Owner, entity.Comp.InputMaterial, -inputAmount, materialStorage, dirty: false);
            _materialStorage.TryChangeMaterialAmount(entity.Owner, entity.Comp.OutputMaterial, inputAmount, materialStorage, dirty: true);
        }
    }
}

