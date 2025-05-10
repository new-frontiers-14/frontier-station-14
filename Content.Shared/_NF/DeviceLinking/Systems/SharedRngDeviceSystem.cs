using Robust.Shared.Timing;

namespace Content.Shared._NF.DeviceLinking.Systems;

// Shared system for RNG device functionality
public abstract class SharedRngDeviceSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    // Generates a random roll and determines the output port based on the number of outputs and target number
    // outputs: Number of possible outputs
    // targetNumber: Target number for percentile dice (1-100)
    // Returns a tuple containing the roll value and the output port
    protected (int roll, int outputPort) GenerateRoll(int outputs, int targetNumber = 50)
    {
        // Use current tick as seed for deterministic randomness
        var rand = new System.Random((int)Timing.CurTick.Value);

        int roll;
        int outputPort;

        if (outputs == 2)
        {
            // For percentile dice, roll 1-100
            roll = rand.Next(1, 101);
            outputPort = roll <= targetNumber ? 1 : 2;
        }
        else
        {
            roll = rand.Next(1, outputs + 1);
            outputPort = roll;
        }

        return (roll, outputPort);
    }
}
