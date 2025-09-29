using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Construction; //Uses base namespace to extend ConstructionSystem behaviour

// Adopted from Wizard's Den's abandoned ConstructionSystem machine part system.

public sealed partial class ConstructionSystem
{
    [Dependency] ExamineSystemShared _examineSystem = default!;

    private void InitializeMachineUpgrades()
    {
        SubscribeLocalEvent<MachineComponent, GetVerbsEvent<ExamineVerb>>(OnMachineExaminableVerb);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));
        if (markup.IsEmpty)
            return; // Not upgradable.

        if(!FormattedMessage.TryFromMarkup(markup.ToMarkup().TrimEnd('\n'), out markup)) // Cursed workaround to https://github.com/space-wizards/RobustToolbox/issues/3371
        {
            markup = FormattedMessage.Empty; // Frontier: attempt sane error handling
        }

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false);
            },
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    // Frontier: return type changed to include stack info
    public List<MachinePartState> GetAllParts(EntityUid uid, MachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<MachinePartState>();

        return GetAllParts(component);
    }

    // Frontier: return type changed to include stack info
    public List<MachinePartState> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartState>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (GetMachinePartState(entity, out var partState))
                parts.Add(partState);
        }

        return parts;
    }

    public bool GetMachinePartState(EntityUid uid, out MachinePartState state)
    {
        state = new MachinePartState();
        MachinePartComponent? part;
        if (TryComp(uid, out part) && part is not null)
            state.Part = part;
        else
            return false;

        TryComp(uid, out state.Stack);
        return true;
    }

    public Dictionary<string, float> GetPartsRatings(List<MachinePartState> partStates)
    {
        var output = new Dictionary<string, float>();
        foreach (var type in _prototypeManager.EnumeratePrototypes<MachinePartPrototype>())
        {
            var amount = 0f;
            var sumRating = 0f;
            foreach (var state in partStates.Where(part => part.Part.PartType == type.ID))
            {
                amount += state.Quantity();
                sumRating += state.Part.Rating * state.Quantity();
            }
            var rating = amount != 0 ? sumRating / amount : 1.0f;
            output.Add(type.ID, rating);
        }

        return output;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        EntityManager.EventBus.RaiseLocalEvent(uid, new RefreshPartsEvent
        {
            Parts = parts,
            PartRatings = GetPartsRatings(parts),
        }, true);
    }
}
