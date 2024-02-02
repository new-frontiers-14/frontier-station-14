namespace Content.Shared.SpaceArtillery;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.Shared.Actions;
using Robust.Shared.Utility;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;



/// <summary>
/// Component dictates if grid is allowed to utilise space artillery armaments at all
/// </summary>
[RegisterComponent]
public sealed partial class SpaceArtilleryGridComponent : Component
{
	/// <summary>
	/// Whether the grid has activated the armaments
	/// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsActive = false;
	
	[ViewVariables]
    public TimeSpan LastActivationTime;
	
	[ViewVariables]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(300);
	
	[ViewVariables]
    public TimeSpan CooldownEndTime;
}