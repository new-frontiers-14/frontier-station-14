using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Dispenser
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class DispenserComponent : Component
    {
        /// <summary>
        /// Used by the server to determine how long the dispenser stays in the "Dispensing" state.
        /// The selected item is dispensed afer this time.
        /// Used by the client to determine how long the dispense animation should be played.
        /// </summary>
        [DataField("dispenseTime")]
        public float DispenseTime = 1f;

        /// <summary>
        /// Default item that is dispensed when the player activates with empty hand
        /// </summary>
        [DataField("defaultItem")]
        public string DefaultItem;

        /// <summary>
        /// A list of input and output items.
        /// When the player activates with a specific input item, dispense the respective output item.
        /// </summary>
        [DataField("inventory", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<string, EntityPrototype>))]
        public Dictionary<string, string> Inventory { get; private set; } = new();

        /// <summary>
        ///     Sound that plays when dispensing an item
        /// </summary>
        [DataField("dispenseSound")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier DispenseSound = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");

        /// <summary>
        ///     Sound that plays when the player activates with an invalid input item
        /// </summary>
        [DataField("denySound")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        public bool Dispensing;
        public string DispensingItemId;
        public float DispenseTimer;
    }
}
