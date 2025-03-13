using Robust.Shared.Timing;

namespace Content.Shared._NF.DeviceLinking.Systems;

/// <summary>
/// Shared system for RNG device functionality
/// </summary>
public abstract class SharedRngDeviceSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _timing = default!;

    /// <summary>
    /// Generates a random roll and determines the output port based on the number of outputs and target number
    /// </summary>
    /// <param name="outputs">Number of possible outputs</param>
    /// <param name="targetNumber">Target number for percentile dice (1-100)</param>
    /// <returns>A tuple containing the roll value and the output port</returns>
    protected (int roll, int outputPort) GenerateRoll(int outputs, int targetNumber = 50)
    {
        // Use current tick as seed for deterministic randomness
        var rand = new System.Random((int)_timing.CurTick.Value);

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
