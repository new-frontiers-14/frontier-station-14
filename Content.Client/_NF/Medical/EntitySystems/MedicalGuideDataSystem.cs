using Content.Client.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Medical.EntitySystems;

public sealed class MedicalGuideDataSystem : SharedMedicalGuideDataSystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<MedicalGuideRegistryChangedEvent>(OnReceiveRegistryUpdate);
    }

    private void OnReceiveRegistryUpdate(MedicalGuideRegistryChangedEvent message)
    {
        Registry = message.Changeset;
    }

    public bool TryGetData(EntProtoId result, out MedicalGuideEntry entry)
    {
        var index = Registry.FindIndex(it => it.Result == result);
        if (index == -1)
        {
            entry = default;
            return false;
        }

        entry = Registry[index];
        return true;
    }
}
