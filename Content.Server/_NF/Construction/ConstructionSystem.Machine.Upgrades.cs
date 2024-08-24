using System.Linq; // Frontier
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes; // Frontier
using Content.Shared.Examine;
using Content.Shared.Interaction.Events; // Frontier
using Content.Shared.Stacks;
using Content.Shared.Verbs; // Frontier
using Robust.Shared.Prototypes; // Frontier
using Robust.Shared.Utility;

namespace Content.Server.Construction; //Uses base namespace to extend ConstructionSystem behaviour

public struct MachinePartState
{
    public MachinePartComponent Part;
    public StackComponent? Stack;
    // If item is a stack, return the count in the stack, otherwise it's a singular, non-stackable part
    public int Quantity()
    {
        return Stack?.Count ?? 1;
    }
}

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
            var rating = amount != 0 ? sumRating / amount : 0;
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
public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartState> Parts = new List<MachinePartState>(); // Frontier: MachinePartComponent<MachinePartState

    public Dictionary<string, float> PartRatings = new Dictionary<string, float>();
}

public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private FormattedMessage _message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        _message = message;
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a percentage-based increase or decrease.
    /// </summary>
    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this._message.TryAddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("percent", percent)) + '\n', out _); // Frontier: AddMarkup<TryAddMarkup
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a numeric increase or decrease.
    /// </summary>
    public void AddNumberUpgrade(string upgradedLocId, int number)
    {
        var difference = Math.Abs(number);
        var locId = number switch
        {
            < 0 => "machine-upgrade-decreased-by-amount",
            0 => "machine-upgrade-not-upgraded",
            > 0 => "machine-upgrade-increased-by-amount",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this._message.TryAddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("difference", difference)) + '\n', out _); // Frontier: AddMarkup<TryAddMarkup
    }
}