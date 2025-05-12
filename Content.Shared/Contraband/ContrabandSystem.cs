using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed class ContrabandSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private bool _contrabandExamineEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandComponent, GetVerbsEvent<ExamineVerb>>(OnDetailedExamine);

        Subs.CVar(_configuration, CCVars.ContrabandExamine, SetContrabandExamine, true);
    }

    public void CopyDetails(EntityUid uid, ContrabandComponent other, ContrabandComponent? contraband = null)
    {
        if (!Resolve(uid, ref contraband))
            return;

        contraband.Severity = other.Severity;
        contraband.AllowedDepartments = other.AllowedDepartments;
        contraband.AllowedDepartments = other.AllowedDepartments;
        contraband.AllowedJobs = other.AllowedJobs;
        contraband.TurnInValues = other.TurnInValues; // Frontier
        contraband.HideValues = other.HideValues; // Frontier
        contraband.HideCarryStatus = other.HideCarryStatus; // Frontier
        Dirty(uid, contraband);
    }

    private void OnDetailedExamine(EntityUid ent,ContrabandComponent component, ref GetVerbsEvent<ExamineVerb> args)
    {

        if (!_contrabandExamineEnabled)
            return;

        // CanAccess is not used here, because we want people to be able to examine legality in strip menu.
        if (!args.CanInteract)
            return;

        if (component.HideValues) // Frontier: allow selective display
            return; // Frontier: allow selective display

        // two strings:
        // one, the actual informative 'this is restricted'
        // then, the 'you can/shouldn't carry this around' based on the ID the user is wearing
        var localizedDepartments = component.AllowedDepartments.Select(p => Loc.GetString("contraband-department-plural", ("department", Loc.GetString(_proto.Index(p).Name))));
        var jobs = component.AllowedJobs.Select(p => _proto.Index(p).LocalizedName).ToArray();
        var localizedJobs = jobs.Select(p => Loc.GetString("contraband-job-plural", ("job", p)));
        var severity = _proto.Index(component.Severity);
        String? departmentExamineMessage = null;
        if (severity.ShowDepartmentsAndJobs)
        {
            //creating a combined list of jobs and departments for the restricted text
            var list = ContentLocalizationManager.FormatList(localizedDepartments.Concat(localizedJobs).ToList());
            // department restricted text
            departmentExamineMessage = Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list));
        }
        // Frontier: 
        // else
        // {
        //     departmentExamineMessage = Loc.GetString(severity.ExamineText);
        // }
        // End Frontier: 

        // text based on ID card
        List<ProtoId<DepartmentPrototype>> departments = new();
        var jobId = "";
        if (_id.TryFindIdCard(args.User, out var id))
        {
            departments = id.Comp.JobDepartments;
            if (id.Comp.LocalizedJobTitle is not null)
            {
                jobId = id.Comp.LocalizedJobTitle;
            }
        }

        String carryingMessage;
        // either its fully restricted, you have no departments, or your departments dont intersect with the restricted departments
        if (departments.Intersect(component.AllowedDepartments).Any()
            || jobs.Contains(jobId))
        {
            carryingMessage = Loc.GetString("contraband-examine-text-in-the-clear");
        }
        else
        {
            // otherwise fine to use :tm:
            carryingMessage = Loc.GetString("contraband-examine-text-avoid-carrying-around");
        }

        var examineMarkup = GetContrabandExamine(Loc.GetString(severity.ExamineText), departmentExamineMessage, carryingMessage, !component.HideCarryStatus); // Frontier: pass HideCarryStatus
        _examine.AddDetailedExamineVerb(args,
            component,
            examineMarkup,
            Loc.GetString("contraband-examinable-verb-text"),
            "/Textures/Interface/VerbIcons/lock.svg.192dpi.png",
            Loc.GetString("contraband-examinable-verb-message"));
    }

    private FormattedMessage GetContrabandExamine(String severity, String? deptMessage, String carryMessage, bool showCarry = true) // Frontier: add showCarry
    {
        var msg = new FormattedMessage();

        // Frontier: severity, department message, hide carry status
        msg.AddMarkupOrThrow(severity);
        if (!string.IsNullOrEmpty(deptMessage))
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(deptMessage);
        }
        if (showCarry)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(carryMessage);
        }
        // End Frontier: severity, department message, hide carry status
        return msg;
    }

    private void SetContrabandExamine(bool val)
    {
        _contrabandExamineEnabled = val;
    }
}
