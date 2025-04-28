using Content.Server._NF.Roadkill.Systems;
using Robust.Shared.Audio;

namespace Content.Server._NF.Roadkill.Components;

[RegisterComponent, Access(typeof(RoadkillSystem))]
public sealed partial class RoadkillComponent : Component
{
    [DataField]
    public float KillSpeed;
    [DataField]
    public float DestroySpeed;
    [DataField]
    public SoundSpecifier? DestroySound;
}
