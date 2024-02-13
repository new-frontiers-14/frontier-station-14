using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.SpaceArtillery; //Frontier code modification

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ShuttleSystem),typeof(SpaceArtillerySystem))] //Frontier modification, added acces to SpaceArtillerySystem
public sealed partial class IFFConsoleComponent : Component
{
    /// <summary>
    /// Flags that this console is allowed to set.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("allowedFlags")]
    public IFFFlags AllowedFlags = IFFFlags.HideLabel;
	
//Frontier Code - allows temporarily disabling IFF console
	/// <summary>
	/// Whether the console should be treated as temporarily disabled
	/// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsDisabled = false;

	/// <summary>
    /// Flags into which console will be forced into
	/// Switches with main flags when armaments are active
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public IFFFlags AccessableAllowedFlags = IFFFlags.None;
//Frontier Code ends here
}
