using Content.Server._NF.Tools.Components;
using Content.Shared.Tools.Components;

namespace Content.Server._NF.Tools;

public sealed class OnToolsUseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OnToolsUseComponent, ToolUseAttemptEvent>(OnToolUseAttempt);
    }

    private void OnToolUseAttempt(EntityUid uid, OnToolsUseComponent component, ToolUseAttemptEvent args)
    {
        if (component.AllToolUseDisabled)
            args.Cancel();

        // Check each tool quality being cancelled.
        foreach (var quality in args.Qualities)
        {
            if (component.DisabledQualities.Contains(quality))
                args.Cancel();
        }
    }
}
