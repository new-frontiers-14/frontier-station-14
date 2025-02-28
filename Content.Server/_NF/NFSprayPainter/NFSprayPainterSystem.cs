using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared._NF.NFSprayPainter;
using Content.Shared._NF.NFSprayPainter.Components;

namespace Content.Server._NF.NFSprayPainter;

/// <summary>
/// Handles spraying pipes using a spray painter.
/// Other are handled in shared.
/// </summary>
public sealed class NFSprayPainterSystem : SharedNFSprayPainterSystem
{
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFSprayPainterComponent, NFSprayPainterPipeDoAfterEvent>(OnPipeDoAfter);

        SubscribeLocalEvent<AtmosPipeColorComponent, InteractUsingEvent>(OnPipeInteract);
    }

    private void OnPipeDoAfter(Entity<NFSprayPainterComponent> ent, ref NFSprayPainterPipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not {} target)
            return;

        if (!TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        Audio.PlayPvs(ent.Comp.SpraySound, ent);

        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<NFSprayPainterComponent>(args.Used, out var painter) || painter.PickedColor is not {} colorName)
            return;

        if (!painter.ColorPalette.TryGetValue(colorName, out var color))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, painter.PipeSprayTime, new NFSprayPainterPipeDoAfterEvent(color), args.Used, target: ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            // multiple pipes can be sprayed at once just not the same one
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true,
        };

        args.Handled = DoAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
