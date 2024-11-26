using Robust.Shared.Audio;

namespace Content.Server.DeltaV.Mail.Components
{
    /// <summary>
    /// This is for the mail teleporter.
    /// Random mail will be teleported to this every few minutes.
    /// </summary>
    [RegisterComponent]
    public sealed partial class MailTeleporterComponent : Component
    {

        // Not starting accumulator at 0 so mail carriers have some deliveries to make shortly after roundstart.
        [DataField]
        public float Accumulator = 1995f; // Frontier 285<1995 (285*7)

        [DataField]
        public TimeSpan TeleportInterval = TimeSpan.FromMinutes(35); // Frontier: 5<35 (5*7)

        /// <summary>
        /// The sound that's played when new mail arrives.
        /// </summary>
        [DataField]
        public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

        /// <summary>
        /// The MailDeliveryPoolPrototype that's used to select what mail this
        /// teleporter can deliver.
        /// </summary>
        [DataField("mailPool")]
        public string MailPool = "RandomNFMailDeliveryPool"; // Frontier: use our own mail pool (TODO: migrate to frontier.yml instance?)

        /// <summary>
        /// How many mail candidates do we need per actual delivery sent when
        /// the mail goes out? The number of candidates is divided by this number
        /// to determine how many deliveries will be teleported in.
        /// It does not determine unique recipients. That is random.
        /// </summary>
        [DataField]
        public int CandidatesPerDelivery = 4; // Frontier: 8<4

        [DataField]
        public int MinimumDeliveriesPerTeleport = 1;

        /// <summary>
        /// Do not teleport any more mail in, if there are at least this many
        /// undelivered parcels.
        /// </summary>
        /// <remarks>
        /// Currently this works by checking how many MailComponent entities
        /// are sitting on the teleporter's tile.
        ///
        /// It should be noted that if the number of actual deliveries to be
        /// made based on the number of candidates divided by candidates per
        /// delivery exceeds this number, the teleporter will spawn more mail
        /// than this number.
        ///
        /// This is just a simple check to see if anyone's been picking up the
        /// mail lately to prevent entity bloat for the sake of performance.
        /// </remarks>
        [DataField]
        public int MaximumUndeliveredParcels = 5;

        /// <summary>
        /// Any item that breaks or is destroyed in less than this amount of
        /// damage is one of the types of items considered fragile.
        /// </summary>
        [DataField]
        public int FragileDamageThreshold = 10;

        /// <summary>
        /// What's the bonus for delivering a fragile package intact?
        /// </summary>
        [DataField]
        public int FragileBonus = 2000; // Frontier 100<2000

        /// <summary>
        /// What's the malus for failing to deliver a fragile package?
        /// </summary>
        [DataField]
        public int FragileMalus = -100;

        /// <summary>
        /// What's the chance for any one delivery to be marked as priority mail?
        /// </summary>
        [DataField]
        public float PriorityChance = 0.07f; // Frontier: 0.1f<0.07f

        /// <summary>
        /// How long until a priority delivery is considered as having failed
        /// if not delivered?
        /// </summary>
        [DataField]
        public TimeSpan PriorityDuration = TimeSpan.FromMinutes(45); // Frontier 5<45

        /// <summary>
        /// What's the bonus for delivering a priority package on time?
        /// </summary>
        [DataField]
        public int PriorityBonus = 5000; // Frontier 250<5000

        /// <summary>
        /// What's the malus for failing to deliver a priority package?
        /// </summary>
        [DataField]
        public int PriorityMalus = -250;

        // Frontier: Large mail
        /// <summary>
        /// What's the bonus for delivering a large package intact?
        /// </summary>
        [DataField]
        public int LargeBonus = 5000; // Frontier: 1500<5000

        /// <summary>
        /// What's the malus for failing to deliver a large package?
        /// </summary>
        [DataField]
        public int LargeMalus = -500;
        // End Frontier: Large mail
    }
}
