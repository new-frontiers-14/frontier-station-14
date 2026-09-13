namespace Content.Server._NF.Speech.Components;

/// <summary>
/// A empty base class for all accents to extend off of, so you can reliably check if a component is an accent
/// </summary>
/// <remarks>
/// The lack of [RegisterComponent] is on purpose, in order to prevent this from causing ECS problems. I hope.
/// (Its probably going to cause problems isn't it ugghhhhh)
/// </remarks>
public abstract partial class BaseAccentComponent : Component
{
    //TODO: Move to shared in order to add it to the few shared accent comps
}
