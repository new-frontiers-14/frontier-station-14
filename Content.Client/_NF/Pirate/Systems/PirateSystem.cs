using Content.Shared._NF.Pirate;
using Robust.Client.GameObjects;

namespace Content.Client._NF.Pirate.Systems;

public sealed partial class PirateSystem : SharedPirateSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
