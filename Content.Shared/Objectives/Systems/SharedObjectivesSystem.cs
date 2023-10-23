using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// Provides API for creating and interacting with objectives.
/// </summary>
public abstract class SharedObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private EntityQuery<MetaDataComponent> _metaQuery;

    public override void Initialize()
    {
        base.Initialize();

        _metaQuery = GetEntityQuery<MetaDataComponent>();
    }

    /// <summary>
    /// Checks requirements and duplicate objectives to see if an objective can be assigned.
    /// </summary>
    public bool CanBeAssigned(EntityUid uid, EntityUid mindId, MindComponent mind, ObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        var ev = new RequirementCheckEvent(mindId, mind);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return false;

        // only check for duplicate prototypes if it's unique
        if (comp.Unique)
        {
            var proto = _metaQuery.GetComponent(uid).EntityPrototype?.ID;
            foreach (var objective in mind.AllObjectives)
            {
                if (_metaQuery.GetComponent(objective).EntityPrototype?.ID == proto)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Spawns and assigns an objective for a mind.
    /// The objective is not added to the mind's objectives, mind system does that in TryAddObjective.
    /// If the objective could not be assigned the objective is deleted and null is returned.
    /// </summary>
    public EntityUid? TryCreateObjective(EntityUid mindId, MindComponent mind, string proto)
    {
        var uid = Spawn(proto);
        if (!TryComp<ObjectiveComponent>(uid, out var comp))
        {
            Del(uid);
            Log.Error($"Invalid objective prototype {proto}, missing ObjectiveComponent");
            return null;
        }

        if (!CanBeAssigned(uid, mindId, mind, comp))
        {
            Log.Warning($"Objective {proto} did not match the requirements for {_mind.MindOwnerLoggingString(mind)}, deleted it");
            return null;
        }

        var ev = new ObjectiveAssignedEvent(mindId, mind);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
        {
            Del(uid);
            Log.Warning($"Could not assign objective {proto}, deleted it");
            return null;
        }

        // let the title description and icon be set by systems
        var afterEv = new ObjectiveAfterAssignEvent(mindId, mind, comp, MetaData(uid));
        RaiseLocalEvent(uid, ref afterEv);

        Log.Debug($"Created objective {ToPrettyString(uid):objective}");
        return uid;
    }

    /// <summary>
    /// Get the title, description, icon and progress of an objective using <see cref="ObjectiveGetInfoEvent"/>.
    /// If any of them are null it is logged and null is returned.
    /// </summary>
    /// <param name="uid"/>ID of the condition entity</param>
    /// <param name="mindId"/>ID of the player's mind entity</param>
    /// <param name="mind"/>Mind component of the player's mind</param>
    public ObjectiveInfo? GetInfo(EntityUid uid, EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return null;

        var ev = new ObjectiveGetProgressEvent(mindId, mind);
        RaiseLocalEvent(uid, ref ev);

        var comp = Comp<ObjectiveComponent>(uid);
        var meta = MetaData(uid);
        var title = meta.EntityName;
        var description = meta.EntityDescription;
        if (comp.Icon == null || ev.Progress == null)
        {
            Log.Error($"An objective {ToPrettyString(uid):objective} of {_mind.MindOwnerLoggingString(mind)} is missing icon or progress ({ev.Progress})");
            return null;
        }

        return new ObjectiveInfo(title, description, comp.Icon, ev.Progress.Value);
    }

    /// <summary>
    /// Sets the objective's icon to the one specified.
    /// Intended for <see cref="ObjectiveAfterAssignEvent"/> handlers to set an icon.
    /// </summary>
    public void SetIcon(EntityUid uid, SpriteSpecifier icon, ObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Icon = icon;
    }
}
