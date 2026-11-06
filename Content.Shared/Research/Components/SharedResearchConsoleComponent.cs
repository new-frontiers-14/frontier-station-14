using Robust.Shared.Serialization;
using Content.Shared._NF.Research; // Frontier

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;

        /// <summary>
        /// Frontier field - all researches and their availablities
        /// </summary>
        public Dictionary<string, ResearchAvailability> Researches;

        public ResearchConsoleBoundInterfaceState(int points, Dictionary<string, ResearchAvailability> researches) // Frontier R&D console rework = researches field
        {
            Points = points;
            Researches = researches; // Frontier R&D console rework
        }
    }
}
