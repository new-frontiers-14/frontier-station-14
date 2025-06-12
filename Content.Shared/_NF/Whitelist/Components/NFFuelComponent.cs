namespace Content.Shared._NF.Whitelist.Components;

/// <summary>
/// Whitelist component for fuel items (AME jars, fuel-grade materials, sheets/ore of fuel mats, jerry cans) to avoid tag redefinition and collisions
/// </summary>
[RegisterComponent]
public sealed partial class NFFuelComponent : Component;
