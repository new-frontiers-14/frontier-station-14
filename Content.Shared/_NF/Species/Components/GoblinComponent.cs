namespace Content.Server._NF.Species.Components;

/// <summary>
/// Whitelist component for goblin species to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinComponent : Component { }

/// <summary>
/// Whitelist component for items crafted by goblins to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinMadeComponent : Component { }

/// <summary>
/// Whitelist component for construction graph for goblin robes to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinClothingRobesComponent : Component { }

/// <summary>
/// Whitelist component for construction graphs using dumpster to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinVehicleComponent : Component { }

/// <summary>
/// Whitelist component for items used in goblin crafts to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinPreciousTrashComponent : Component { }

/// <summary>
/// Whitelist component for construction graphs using trash bags to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class GoblinPreciousTrashBagComponent : Component { }
