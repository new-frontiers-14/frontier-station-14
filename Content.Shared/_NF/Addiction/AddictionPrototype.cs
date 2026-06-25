using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._NF.Addiction;

[Prototype, DataDefinition]
public sealed partial class AddictionPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AddictionPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// How long of a period in must the entity not get this AddictionEffect for its addiction rating to drop. Defaults to 60 seconds
    /// </summary>
    [ViewVariables, DataField]
    public TimeSpan CheckPeriod { get; set; } = TimeSpan.FromMinutes(0.5);

    /// <summary>
    /// At what addiction rating does the entity start to have withdrawal effects and is considered addicted
    /// </summary>
    [ViewVariables, DataField(required: true)]
    public int Threshold { get; set; }

    /// <summary>
    /// How much to multiply the addiction rating by when the check period happens, should be less than 1 and greater than or equal to 0
    /// </summary>
    [ViewVariables, DataField(required: true)]
    public float DecayRate { get; set; }

    [DataField]
    public WithdrawalData Withdrawal { get; private set; }


}
