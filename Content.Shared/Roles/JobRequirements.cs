using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public static class JobRequirements
{
    public static bool TryRequirementsMet(
        JobPrototype job,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile)
    {
        var sys = entManager.System<SharedRoleSystem>();
        var requirements = sys.GetJobRequirement(job);
        reason = null;
        if (requirements == null)
            return true;


        // Frontier: add alternate requirement sets
        bool success = true;
        foreach (var requirement in requirements)
        {
            if (!requirement.Check(entManager, protoManager, profile, playTimes, out reason))
            {
                success = false;
                break;
            }
        }
        if (success)
            return true;

        var altRequirementsSets = sys.GetAlternateJobRequirements(job) ?? new();
        foreach (var requirementSet in altRequirementsSets.Values)
        {
            success = true;
            foreach (var requirement in requirementSet)
            {
                // Frontier: do not accumulate reasons for alternate job requirements.
                if (!requirement.Check(entManager, protoManager, profile, playTimes, out _))
                {
                    success = false;
                    break;
                }
            }
            if (success)
                return true;
        }

        // If this happens, something's gone wrong.  Only for error suppression.
        if (reason == null)
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-no-reason-given"));

        // Frontier: check alternate requirement times
        return false;

    }
}

/// <summary>
/// Abstract class for playtime and other requirements for role gates.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class JobRequirement
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason);
}
