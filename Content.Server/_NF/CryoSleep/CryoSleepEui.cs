using Content.Server.EUI;
using Content.Shared._NF.CryoSleep;
using Content.Shared.Eui;

namespace Content.Server._NF.CryoSleep;

public sealed class CryoSleepEui : BaseEui
{
    private readonly CryoSleepSystem _cryoSystem;
    private readonly EntityUid _body;
    private readonly EntityUid _cryopod;

    public CryoSleepEui(EntityUid body, EntityUid cryopod, CryoSleepSystem cryoSys)
    {
        _body = body;
        _cryopod = cryopod;
        _cryoSystem = cryoSys;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptCryoChoiceMessage choice)
        {
            Close();
            return;
        }

        if (_body is { Valid: true })
        {
            if (choice.Button == AcceptCryoUiButton.Accept)
            {
                _cryoSystem.CryoStoreBody(_body, _cryopod);
            }
            else
            {
                _cryoSystem.EjectBody(_cryopod, body: _body);
            }
        }

        Close();
    }
}
