using Content.Shared.Kitchen.Components;

namespace Content.Client.Kitchen.Components
{
    [RegisterComponent]
    // Unnecessary line: [ComponentReference(typeof(SharedDeepFryerComponent))]
    public sealed partial class DeepFryerComponent : SharedDeepFryerComponent
    {
    }
}
