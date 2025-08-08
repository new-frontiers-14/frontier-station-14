using Content.Shared._NF.Cargo;

namespace Content.Client._NF.Cargo.Systems;

public sealed partial class NFCargoSystem : SharedNFCargoSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeCargoTelepad();
    }
}
