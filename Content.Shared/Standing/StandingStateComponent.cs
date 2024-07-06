using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Standing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(StandingStateSystem))]
    public sealed partial class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public SoundSpecifier DownSound { get; private set; } = new SoundCollectionSpecifier("BodyFall");

        [DataField, AutoNetworkedField]
        public bool Standing { get; set; } = true;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField, AutoNetworkedField]
        public List<string> ChangedFixtures = new();

        [DataField]
        public EntProtoId LieDownAction = "ActionLieDown";

        [DataField, AutoNetworkedField]
        public EntityUid? LieDownActionEntity;

        [DataField("stand-up-action")]
        public EntProtoId StandUpAction = "ActionStandUp";

        [DataField, AutoNetworkedField]
        public EntityUid? StandUpActionEntity;

        [DataField]
        public TimeSpan Delay = TimeSpan.FromSeconds(2);

        [DataField]
        public TimeSpan LastUsage = TimeSpan.FromSeconds(2);
    }
}

public sealed partial class LieDownActionEvent : InstantActionEvent {}
public sealed partial class StandUpActionEvent : InstantActionEvent {}

