using Content.Shared._NF.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._NF.DeviceLinking.Systems;

// Shared system for RNG device functionality
public abstract class SharedRngDeviceSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _gameTiming = default!;

    private static readonly int[] ValidOutputCounts = { 2, 4, 6, 8, 10, 12, 20 };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RngDeviceComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    // Checks if the output count is valid
    public static bool IsValidOutputCount(int outputs)
    {
        return Array.IndexOf(ValidOutputCounts, outputs) >= 0;
    }

    // Gets the device type name based on the number of outputs
    public string GetDeviceType(EntityUid uid, RngDeviceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return "Unknown";

        try
        {
            string prefix = GetStatePrefix(uid, component);
            if (string.IsNullOrEmpty(prefix))
                return "Unknown";

            // Use string manipulation instead of char operations to avoid ReadOnlySpan<char>
            if (prefix.Length > 0)
            {
                return prefix.Substring(0, 1).ToUpperInvariant() + prefix.Substring(1);
            }

            return prefix;
        }
        catch (ArgumentException)
        {
            return "Unknown";
        }
    }

    // Gets the state prefix for visual updates
    public string GetStatePrefix(EntityUid uid, RngDeviceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            throw new ArgumentException($"Entity {uid} does not have RngDeviceComponent");

        if (!IsValidOutputCount(component.Outputs))
            throw new ArgumentException($"Unsupported number of outputs: {component.Outputs}");

        if (component.Outputs == 2)
            return "percentile";

        return $"d{component.Outputs}";
    }

    // Performs a deterministic roll based on the current tick
    public (int roll, int outputPort) PerformRoll(Entity<RngDeviceComponent> ent)
    {
        var comp = ent.Comp;

        // Use current tick as seed for deterministic randomness
        // Both client and server will use the same seed in order to get the same results
        var seed = (int)_gameTiming.CurTick.Value;

        // XOR with entity ID to ensure different entities get different results on the same tick
        var random = new System.Random(seed ^ ent.Owner.GetHashCode());

        int roll;
        int outputPort;

        if (comp.Outputs == 2)
        {
            // For percentile dice, roll 1-100
            roll = random.Next(1, 101);
            outputPort = roll <= comp.TargetNumber ? 1 : 2;
        }
        else
        {
            roll = random.Next(1, comp.Outputs + 1);
            outputPort = roll;
        }

        // Store the values for future reference
        comp.LastRoll = roll;
        comp.LastOutputPort = outputPort;

        return (roll, outputPort);
    }

    // Gets the ProtoId for the specified output port number
    protected static ProtoId<SourcePortPrototype> GetOutputPort(RngDeviceComponent comp, int portNumber)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        return comp.OutputPorts[portNumber];
    }

    // Creates an array of output port ProtoIds for the given number of outputs
    protected static ProtoId<SourcePortPrototype>[] CreatePortsArray(RngDeviceComponent comp, int count)
    {
        var result = new ProtoId<SourcePortPrototype>[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = comp.OutputPorts[i + 1];
        }
        return result;
    }

    private void OnExamined(Entity<RngDeviceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", ent.Comp.LastRoll)));

        if (ent.Comp.Outputs == 2)  // Only show port info for percentile die
            args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", ent.Comp.LastOutputPort)));
    }

    private void OnAfterAutoHandleState(Entity<RngDeviceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Update the state prefix when component state changes
        ent.Comp.StatePrefix = GetStatePrefix(ent, ent.Comp);

        // Update visuals if needed
        UpdateVisuals(ent);
    }

    // Updates the visual state of the RNG device
    protected virtual void UpdateVisuals(Entity<RngDeviceComponent> ent)
    {
        // Implemented in client system
    }
}
