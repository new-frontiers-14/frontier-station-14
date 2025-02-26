using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._NF.NFSprayPainter.Components;
using Content.Shared._NF.NFSprayPainter.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._NF.NFSprayPainter;

/// <summary>
/// System for painting airlocks using a spray painter.
/// Pipes are handled serverside since AtmosPipeColorSystem is server only.
/// </summary>
public abstract class SharedNFSprayPainterSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public Dictionary<string, PaintableTargets> Targets { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        CacheStyles();

        SubscribeLocalEvent<NFSprayPainterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NFSprayPainterComponent, NFSprayPainterDoAfterEvent>(OnPaintableDoAfter);
        Subs.BuiEvents<NFSprayPainterComponent>(NFSprayPainterUiKey.Key, subs =>
        {
            subs.Event<NFSprayPainterSpritePickedMessage>(OnSpritePicked);
            subs.Event<NFSprayPainterColorPickedMessage>(OnColorPicked);
        });

        SubscribeLocalEvent<NFPaintableComponent, InteractUsingEvent>(OnPaintableInteract);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnMapInit(Entity<NFSprayPainterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ColorPalette.Count == 0)
            return;

        foreach (var target in Targets.Keys.ToList())
        {
            ent.Comp.Indexes[target] = 0;
        }

        SetColor(ent, ent.Comp.ColorPalette.First().Key);
    }

    private void OnPaintableDoAfter(Entity<NFSprayPainterComponent> ent, ref NFSprayPainterDoAfterEvent args)
    {
        ent.Comp.DoAfters.Remove(args.Category);

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<NFPaintableComponent>(target, out var paintable))
            return;

        Dirty(target, paintable);

        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        Appearance.SetData(target, args.Visuals, args.Data);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    #region UI messages

    private void OnColorPicked(Entity<NFSprayPainterComponent> ent, ref NFSprayPainterColorPickedMessage args)
    {
        SetColor(ent, args.Key);
    }

    private void OnSpritePicked(Entity<NFSprayPainterComponent> ent, ref NFSprayPainterSpritePickedMessage args)
    {
        ent.Comp.Indexes[args.Category] = args.Index;
        Dirty(ent, ent.Comp);
    }

    private void SetColor(Entity<NFSprayPainterComponent> ent, string? paletteKey)
    {
        if (paletteKey == null || paletteKey == ent.Comp.PickedColor)
            return;

        if (!ent.Comp.ColorPalette.ContainsKey(paletteKey))
            return;

        ent.Comp.PickedColor = paletteKey;
        Dirty(ent, ent.Comp);
    }

    #endregion

    private void OnPaintableInteract(Entity<NFPaintableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<NFSprayPainterComponent>(args.Used, out var painter))
            return;

        var group = Proto.Index(ent.Comp.Group);

        // idk why it's necessary, but let it be.
        if (!group.Duplicates && painter.DoAfters.TryGetValue(group.Category, out _))
            return;

        if (!Targets.TryGetValue(group.Category, out var target))
            return;

        var selected = painter.Indexes.GetValueOrDefault(group.Category, 0);
        var style = target.Styles[selected];
        if (!group.AppearanceData.TryGetValue(style, out var proto))
        {
            var msg = Loc.GetString("spray-painter-style-not-available");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        var time = target.Time;
        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            time,
            new NFSprayPainterDoAfterEvent(proto.Data, group.Category, target.Visuals),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out var id))
            return;

        // since we are now spraying an airlock prevent spraying more at the same time
        // pipes ignore this
        painter.DoAfters[group.Category] = (DoAfterId)id;
        args.Handled = true;

        // Log the attempt
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} to '{style}' at {Transform(ent).Coordinates:targetlocation}");
    }

    #region Style caching

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<NFPaintableGroupPrototype>())
            return;

        Targets.Clear();
        CacheStyles();
    }

    protected virtual void CacheStyles()
    {
        foreach (var proto in Proto.EnumeratePrototypes<NFPaintableGroupPrototype>())
        {
            var targetExists = Targets.ContainsKey(proto.Category);

            SortedSet<string> styles = targetExists
                ? new(Targets[proto.Category].Styles)
                : new();
            var groups = targetExists
                ? Targets[proto.Category].Groups
                : new();

            groups.Add(proto);
            foreach (var style in proto.AppearanceData.Keys)
            {
                styles.Add(style);
            }

            Targets[proto.Category] = new(styles.ToList(), groups, proto.Visuals, proto.State, proto.Time);
        }
    }

    #endregion
}

public record PaintableTargets(List<string> Styles, List<NFPaintableGroupPrototype> Groups, NFPaintableVisuals Visuals, string? State, float Time);
