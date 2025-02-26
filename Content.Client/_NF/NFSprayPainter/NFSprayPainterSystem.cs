using System.Linq;
using Content.Shared._NF.NFSprayPainter;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.NFSprayPainter;

public sealed class NFSprayPainterSystem : SharedNFSprayPainterSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public Dictionary<string, List<NFSprayPainterEntry>> Entries { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        foreach (var (category, target) in Targets)
        {
            Entries.Add(category, new());

            foreach (string style in target.Styles)
            {
                var group = target.Groups
                    .FindAll(x => x.AppearanceData.ContainsKey(style))
                    .MaxBy(x => x.IconPriority);

                if (group == null ||
                    !group.AppearanceData.TryGetValue(style, out var protoId) ||
                    !_prototypeManager.TryIndex(protoId.DisplayEntity, out var proto))
                {
                    Entries[category].Add(new NFSprayPainterEntry(style, null));
                    continue;
                }

                Entries[category].Add(new NFSprayPainterEntry(style, proto));
            }
        }
    }
}

public sealed class NFSprayPainterEntry
{
    public string Name;
    public EntityPrototype? Proto;

    public NFSprayPainterEntry(string name, EntityPrototype? proto)
    {
        Name = name;
        Proto = proto;
    }
}
