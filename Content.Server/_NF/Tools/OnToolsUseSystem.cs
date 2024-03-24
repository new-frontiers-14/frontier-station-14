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
        // prevent deconstruct
        if (component.Disabled)
            args.Cancel();
    }
}
