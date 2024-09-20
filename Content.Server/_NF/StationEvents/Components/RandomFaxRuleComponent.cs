using Content.Server.StationEvents.Events;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomFaxRule))]
public sealed partial class RandomFaxRuleComponent : Component
{
    /// <summary>
    /// FaxPrintout fields.  All strings apart from PrototypeId will be localized
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string? Label { get; private set; }

    [DataField(required: true)]
    public string Content { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId PrototypeId { get; private set; } = default!;

    [DataField]
    public string? StampState { get; private set; }

    [DataField]
    public List<StampDisplayInfo>? StampedBy { get; private set; } = new();

    [DataField]
    public bool Locked { get; private set; }

    /// <summary>
    /// The localized string
    /// </summary>
    [DataField]
    public string? FromAddress;

    // TODO: run arbitrary functions 

    /// <summary>
    ///     All the valid IWireActions currently in this layout.
    /// </summary>
    [DataField]
    public List<IPreFaxAction>? PreFaxActions { get; private set; }

    /// <summary>
    ///     All the valid IWireActions currently in this layout.
    /// </summary>
    [DataField]
    public List<IRecipientFaxAction>? PerRecipientActions { get; private set; }

    /// <summary>
    ///     Minimum faxes to send
    /// </summary>
    [DataField]
    public int MinFaxes { get; private set; } = 1;

    /// <summary>
    ///     Maximum faxes to send
    /// </summary>
    [DataField]
    public int MaxFaxes { get; private set; } = 1;
}

// TODO: relocate these definitions.
public interface IPreFaxAction
{
    /// <summary>
    ///     Initializes the action. Intended to setup resources, but the action should not be stateful.
    /// </summary>
    public void Initialize();

    /// <summary>
    ///     Formats a fax printout with general information (target station)
    /// </summary>
    public void Format(EntityUid station, ref EditableFaxPrintout printout, ref string? fromAddress);
}

public interface IRecipientFaxAction
{
    /// <summary>
    ///     Initializes the action. Intended to setup resources, but the action should not be stateful.
    /// </summary>
    public void Initialize();

    /// <summary>
    ///     Formats a fax printout with recipient-specific information (target station, fax machine entity)
    /// </summary>
    public void Format(EntityUid station, EntityUid fax, FaxMachineComponent faxComponent, ref EditableFaxPrintout printout, ref string? fromAddress);
}

public sealed partial class EditableFaxPrintout
{
    public string Name = default!;
    public string? Label;
    public string Content = default!;
    public string PrototypeId = default!;
    public string? StampState;
    public List<StampDisplayInfo> StampedBy = new();
    public bool Locked;
}