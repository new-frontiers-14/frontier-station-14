namespace Content.Server._NF.Speech.Components;

/// <summary>
/// A empty base class for all accents to extend off of, so you can reliably check if a component is an accent
/// </summary>
/// <remarks>
/// The lack of [RegisterComponent] is on purpose, in order to prevent this from causing ECS problems. I hope.
/// (Its probably going to cause problems isn't it ugghhhhh)
/// </remarks>
public abstract partial class AccentBase : Component
{
    //TODO: Add AccentBase to ALL AccentComponents.
    //This is going to take a hot minute so I'm doing it last, I'll have it added to just Replacement and Monkey for now
}
