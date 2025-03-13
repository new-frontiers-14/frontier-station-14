using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class RetractorComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a retractor";
    public bool? Used { get; set; } = null;
    
    /// <summary>
    ///     Multiply the step's doafter by this value.
    /// </summary>
    [DataField]
    public float Speed { get; set; } = 1f;
}