using Content.Server._DV.Speech.EntitySystems;
using Content.Server._NF.Speech.Components;

namespace Content.Server._DV.Speech.Components;

[RegisterComponent]
[Access(typeof(ScottishAccentSystem))]
public sealed partial class ScottishAccentComponent : BaseAccentComponent //Frontier: Component<BaseAccentComponent
{ }
