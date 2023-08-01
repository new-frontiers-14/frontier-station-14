using Content.Client._NF.M_Emp.UI;
//using Content.Shared._NF.M_Emp.BUI;
using Robust.Client.GameObjects;
using Content.Shared._NF.M_Emp;

namespace Content.Client._NF.M_Emp.BUI;

public sealed class M_EmpBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private M_EmpMenu? _menu;

    [Dependency] private readonly SharedM_EmpSystem _memp = default!;

    public M_EmpBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new M_EmpMenu();
        _menu.RequestRequested += OnRequest;
        _menu.ActivateRequested += OnActivate;
        _menu.OnClose += Close;

        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _menu?.Dispose();
        }
    }

    private void OnRequest()
    {
        //SendMessage(new M_EmpRequestMessage());
    }

    private void OnActivate()
    {

    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

       // if (state is not M_EmpInterfaceState palletState)
       //     return;

        //_menu?.SetEnabled(palletState.Enabled);
        //_menu?.SetAppraisal(palletState.Appraisal);
        //_menu?.SetCount(palletState.Count);
    }
}
