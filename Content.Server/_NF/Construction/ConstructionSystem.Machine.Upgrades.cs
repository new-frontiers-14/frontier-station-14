using System.Linq; // Frontier
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes; // Frontier
using Content.Shared.Examine;
using Content.Shared.Interaction.Events; // Frontier
using Content.Shared.Verbs; // Frontier
using Robust.Shared.Prototypes; // Frontier
using Robust.Shared.Utility;

namespace Content.Server.Construction; //Uses base namespace to extend ConstructionSystem behaviour

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

        markup = FormattedMessage.FromMarkup(markup.ToMarkup().TrimEnd('\n')); // Cursed workaround to https://github.com/space-wizards/RobustToolbox/issues/3371

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

    public List<MachinePartComponent> GetAllParts(EntityUid uid, MachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<MachinePartComponent>();

        return GetAllParts(component);
    }

    public List<MachinePartComponent> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartComponent>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent>(entity, out var machinePart))
                parts.Add(machinePart);
        }

        return parts;
    }

    public Dictionary<string, float> GetPartsRatings(List<MachinePartComponent> parts)
    {
        var output = new Dictionary<string, float>();
        foreach (var type in _prototypeManager.EnumeratePrototypes<MachinePartPrototype>())
        {
            var amount = 0f;
            var sumRating = 0f;
            foreach (var part in parts.Where(part => part.PartType == type.ID))
            {
                amount++;
                sumRating += part.Rating;
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
