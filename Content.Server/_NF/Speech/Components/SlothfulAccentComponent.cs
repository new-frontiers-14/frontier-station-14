namespace Content.Server._NF.Speech.Components;

/// <summary>
/// A mixture of BleatingAccent and SlowAccent, but modified to repeat vowels no matter which consonant came first.
/// It also varies the frequency of repeating letters and delaying words.
/// </summary>
[RegisterComponent]
public sealed partial class SlothfulAccentComponent : Component;
