using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFConsoleComponent, AnchorStateChangedEvent>(OnIFFConsoleAnchor);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowIFFMessage>(OnIFFShow);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowVesselMessage>(OnIFFShowVessel);
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent ev)
    {
        var splitMass = _cfg.GetCVar(CCVars.HideSplitGridsUnder);

        if (splitMass < 0)
            return;

        foreach (var grid in ev.NewGrids)
        {
            if (!_physicsQuery.TryGetComponent(grid, out var physics) ||
                physics.Mass > splitMass)
            {
                continue;
            }

            AddIFFFlag(grid, IFFFlags.HideLabel);
        }
    }

    private void OnIFFShow(EntityUid uid, IFFConsoleComponent component, IFFShowIFFMessage args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid == null ||
            (component.AllowedFlags & IFFFlags.HideLabel) == 0x0)
        {
            return;
        }

        if (!args.Show)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
    }

    private void OnIFFShowVessel(EntityUid uid, IFFConsoleComponent component, IFFShowVesselMessage args)
    {
        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid is not { } grid || //Frontier: IFF HEAT System Edits START
            (component.AllowedFlags & IFFFlags.Hide) == 0x0)
        {
            return;
        }

        if (!TryComp(xform.GridUid.Value, out ShuttleComponent? shuttle))
            return;

        if (!args.Show)
        {
            if (shuttle.HeatCapacity - shuttle.CurrentHeat < shuttle.HeatGeneration)
                return;
            AddIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
            shuttle.Active = true;
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
            shuttle.Active = false; //Frontier: IFF HEAT System END
        }
    }

    private void OnIFFConsoleAnchor(EntityUid uid, IFFConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        // If we anchor / re-anchor then make sure flags up to date.
        if (!args.Anchored ||
            !TryComp(uid, out TransformComponent? xform) || xform.GridUid is not { } grid || //Frontier
            !TryComp<IFFComponent>(grid, out var iff) || //Frontier
            !TryComp<ShuttleComponent>(grid, out var shuttle)) //Frontier
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = IFFFlags.None,
                HeatCapacity = 0f, //Frontier
                CurrentHeat = 0f, //Frontier
            });
        }
        else
        {
            _uiSystem.SetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = iff.Flags,
                HeatCapacity = shuttle.HeatCapacity, //Frontier
                CurrentHeat = shuttle.CurrentHeat, //Frontier
            });
        }
    }

    public void UpdateIFFInterface(EntityUid console, IFFConsoleComponent comp)
    {
        if (!TryComp<TransformComponent>(console, out var xform) || xform.GridUid is not { } grid ||
            !TryComp<IFFComponent>(grid, out var iff) ||
            !TryComp<ShuttleComponent>(grid, out var shuttle))
        {
            return;
        }

        _uiSystem.SetUiState(
            console,
            IFFConsoleUiKey.Key,
            new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = comp.AllowedFlags,
                Flags = iff.Flags,
                HeatCapacity = shuttle.HeatCapacity,
                CurrentHeat = shuttle.CurrentHeat, //Frontier: IFF HEAT System Edits END
            });
    }
}
