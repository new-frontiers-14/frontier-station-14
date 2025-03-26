using Robust.Shared.GameStates;

namespace Content.Shared._DV.TapeRecorder.Components;

/// <summary>
/// Added to tape records that are updating, winding or rewinding the tape.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveTapeRecorderComponent : Component;
