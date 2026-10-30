using Content.Server._NF.Speech.Components;
using Content.Server.Speech.EntitySystems;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(SouthernAccentSystem))]
public sealed partial class SouthernAccentComponent : BaseAccentComponent //Frontier: Component<BaseAccentComponent
{ }
