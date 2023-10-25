using System.Numerics;
using Content.Server._NF.M_Emp;
using Content.Server.Shuttles.Systems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent]
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
        /// Next time we tick damage for anyone colliding.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("nextFire", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextFire;

        [DataField("machinePartThrust", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartThrust = "Capacitor";

        [DataField("partRatingThrustMultiplier")]
        public float PartRatingThrustMultiplier = 1.5f;

        [DataField("thrusterIgnoreEmp")]
        public bool ThrusterIgnoreEmp = false;

        /// <summary>
        ///     While disabled by EMP
        /// </summary>
        [DataField("timeoutFromEmp", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan TimeoutFromEmp = TimeSpan.Zero;

        [DataField("disableDuration"), ViewVariables(VVAccess.ReadWrite)]
        public float DisableDuration = 60f;
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
