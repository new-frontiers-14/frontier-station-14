using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Starlight;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

public enum CyberneticImplantType
{
    Undefined,
    Limb,
    Organ
}

[Serializable, NetSerializable]
public struct CyberneticImplant
{
    public string ID;
    public string Name;
    public int Cost;
    public CyberneticImplantType Type;
    public List<string> AttachedParts;

    public static List<CyberneticImplant> GetAllCybernetics(IPrototypeManager prototypeManager)
    {
        return prototypeManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract)
            .Where(p => p.Components.TryGetValue("RoundstartImplantable", out _))
            .Select(p =>
            {
                if (p.Components.TryGetValue("RoundstartImplantable", out var implant)
                    && implant.Component is RoundstartImplantableComponent implantComp
                    && p.Parents is not null)
                {
                    return new CyberneticImplant
                    {
                        ID = p.ID,
                        Name = p.Name,
                        Cost = implantComp.Cost,
                        Type = p.Parents.Contains("PartCyber") ? CyberneticImplantType.Limb
                            : p.Parents.Contains("OrganCyber") ? CyberneticImplantType.Organ
                            : CyberneticImplantType.Undefined,
                        AttachedParts = p.Components.TryGetValue("WithAttachedBodyParts", out var parts)
                            && parts.Component is WithAttachedBodyPartsComponent partComp
                                ? partComp.Parts.Values.Select(p => (string) p).Distinct().ToList()
                                : []
                    };
                }

                return new CyberneticImplant { ID = "broken" };
            })
            .Where(p => p.ID != "broken")
            .Where(p => p.Type != CyberneticImplantType.Undefined)
            .ToList();
    }

    public static HumanoidVisualLayers LayerFromBodypart(BodyPartComponent part)
    {
        return (part.PartType, part.Symmetry) switch
        {
            (BodyPartType.Arm, BodyPartSymmetry.Left) => HumanoidVisualLayers.LArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => HumanoidVisualLayers.RArm,
            (BodyPartType.Hand, BodyPartSymmetry.Left) => HumanoidVisualLayers.LHand,
            (BodyPartType.Hand, BodyPartSymmetry.Right) => HumanoidVisualLayers.RHand,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => HumanoidVisualLayers.LLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => HumanoidVisualLayers.RLeg,
            (BodyPartType.Foot, BodyPartSymmetry.Left) => HumanoidVisualLayers.LFoot,
            (BodyPartType.Foot, BodyPartSymmetry.Right) => HumanoidVisualLayers.RFoot,
            _ => HumanoidVisualLayers.Special,
        };
    }

    public static string SlotIDFromBodypart(BodyPartComponent part)
    {
        var slot = part.Symmetry switch
        {
            BodyPartSymmetry.Left => "left ",
            BodyPartSymmetry.Right => "right ",
            _ => ""
        };

        slot += part.PartType switch
        {
            BodyPartType.Arm => "arm",
            BodyPartType.Leg => "leg",
            BodyPartType.Hand => "hand",
            BodyPartType.Foot => "foot",
            _ => ""
        };

        return slot;
    }
}
