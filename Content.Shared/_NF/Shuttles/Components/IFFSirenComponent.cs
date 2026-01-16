namespace Content.Shared._NF.Shuttles.Components;

/// <summary>
/// Component added to shuttles that are able to set a IFF Siren.
/// Usually added to medical and security shuttles.
///
/// IFF color will blend between two colors within a set timeframe.
/// </summary>
[RegisterComponent]
public sealed partial class IFFSirenComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color InitalColor = Color.Red;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color FinalColor = Color.Blue;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BlendLength;
}
