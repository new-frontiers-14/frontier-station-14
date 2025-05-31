using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Item.ItemToggle.Components;

/// <summary>
/// Adds or removes components when toggled.
/// Requires <see cref="ComponentCyclerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ComponentCyclerComponent : Component
{
    [DataDefinition]
    public sealed partial class CompEntry
    {
        [DataField(required: true)]
        public ComponentRegistry Components = new();

        [DataField]
        public SoundSpecifier? UseSound;

        [DataField]
        public SoundSpecifier? ChangeSound;

        [DataField]
        public SpriteSpecifier? Sprite;

        [DataField]
        public string QualityName = string.Empty;
    }

    [DataField(required: true)]
    public CompEntry[] Entries { get; private set; } = Array.Empty<CompEntry>();

    [ViewVariables]
    [AutoNetworkedField]
    public int CurrentEntry = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool UiUpdateNeeded;

    [DataField]
    public bool StatusShowBehavior = true;
}
