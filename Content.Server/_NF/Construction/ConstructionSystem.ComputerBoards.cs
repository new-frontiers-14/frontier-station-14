using Content.Shared._NF.Construction.Components;
using Content.Shared.Examine;

namespace Content.Server.Construction; //Uses base namespace to extend ConstructionSystem behaviour

public sealed partial class ConstructionSystem
{
    private void InitializeComputerBoards()
    {
        SubscribeLocalEvent<ComputerTabletopBoardComponent, ExaminedEvent>(OnTabletopExamined);
        SubscribeLocalEvent<ComputerWallmountBoardComponent, ExaminedEvent>(OnWallmountExamined);
    }

    private void OnTabletopExamined(Entity<ComputerTabletopBoardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("computer-tabletop-board-examine"));
    }

    private void OnWallmountExamined(Entity<ComputerWallmountBoardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("computer-wallmount-board-examine"));
    }
}
