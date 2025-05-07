using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking; // Frontier
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
    [Access(typeof(ThrusterSystem))]
    public sealed partial class ThrusterComponent : Component
    {
        /// <summary>
        /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool IsOn;

        // Need to serialize this because RefreshParts isn't called on Init and this will break post-mapinit maps!
        [ViewVariables(VVAccess.ReadWrite), DataField("thrust")]
        public float Thrust = 100f;

        [DataField("baseThrust"), ViewVariables(VVAccess.ReadWrite)]
        public float BaseThrust = 100f;

        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        [DataField("burnShape")] public List<Vector2> BurnPoly = new()
        {
            new Vector2(-0.4f, 0.5f),
            new Vector2(-0.1f, 1.2f),
            new Vector2(0.1f, 1.2f),
            new Vector2(0.4f, 0.5f)
        };

        /// <summary>
        /// How much damage is done per second to anything colliding with our thrust.
        /// </summary>
        [DataField("damage")] public DamageSpecifier? Damage = new();

        [DataField("requireSpace")]
        public bool RequireSpace = true;

        // Used for burns

        public List<EntityUid> Colliding = new();

        public bool Firing = false;

        /// <summary>
        /// How often thruster deals damage.
        /// </summary>
        [DataField]
        public TimeSpan FireCooldown = TimeSpan.FromSeconds(2);

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
        public TimeSpan NextFire = TimeSpan.Zero;

        // Frontier: upgradeable parts, togglable thrust
        [DataField]
        public ProtoId<MachinePartPrototype> MachinePartThrust = "Capacitor";

        [DataField]
        public float[] ThrustPerPartLevel = [130, 170, 210, 250];

        /// <summary>
        /// Load on the power network, in watts.
        /// </summary>
        public float OriginalLoad { get; set; } = 0;

        /// <summary>
        /// Togglable thrusters
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OnPort = "On";

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OffPort = "Off";

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string TogglePort = "Toggle";
        // End Frontier: upgradeable parts, togglable thrust
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
