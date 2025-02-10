namespace Content.Shared._NF.Solar.Components;

[RegisterComponent]
public sealed partial class SolarPoweredGridComponent : Component
{
    [DataField]
    public Angle TargetPanelRotation = Angle.Zero;

    [DataField]
    public Angle TargetPanelVelocity = Angle.Zero;

    [DataField]
    public float TotalPanelPower = 0;

    [DataField]
    public uint LastUpdatedTick = 0;
}
