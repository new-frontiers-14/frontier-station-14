namespace Content.Server._NF.Lander;

[RegisterComponent]
public sealed partial class LanderComponent : Component
{
    [DataField]
    public EntityUid MotherStation;
}
