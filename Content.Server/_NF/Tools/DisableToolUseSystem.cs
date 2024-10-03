using Content.Server._NF.Tools.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._NF.Tools;

public sealed class DisableToolUseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisableToolUseComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
    }

    private void OnToolUseAttempt(EntityUid uid, DisableToolUseComponent component, ToolUseAttemptEvent args)
    {
        // Check each tool quality being cancelled.
        foreach (var quality in args.Qualities)
        {
            if (Disabled(component, quality))
                args.Cancel();
        }
    }

    private bool Disabled(DisableToolUseComponent component, ProtoId<ToolQualityPrototype> quality)
    {
        switch (quality)
        {
            case "Anchoring":
                return component.Anchoring;
            case "Prying":
                return component.Prying;
            case "Screwing":
                return component.Screwing;
            case "Cutting":
                return component.Cutting;
            case "Welding":
                return component.Welding;
            case "Pulsing":
                return component.Pulsing;
            case "Slicing":
                return component.Slicing;
            case "Sawing":
                return component.Sawing;
            case "Honking":
                return component.Honking;
            case "Rolling":
                return component.Rolling;
            case "Digging":
                return component.Digging;
            default:
                return false;
        }
    }
}
