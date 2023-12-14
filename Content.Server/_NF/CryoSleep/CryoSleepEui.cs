using Content.Server.EUI;
using Content.Shared.CryoSleep;
using Content.Shared.Eui;

namespace Content.Server.CryoSleep;

public sealed class CryoSleepEui : BaseEui
{
    private readonly CryoSleepSystem _cryoSystem;
    private readonly EntityUid _mind;
    private readonly EntityUid _cryopod;

    public CryoSleepEui(EntityUid mind, EntityUid cryopod, CryoSleepSystem cryoSys)
    {
        _mind = mind;
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

        if (_mind is { Valid: true } body)
        {
            if (choice.Button == AcceptCryoUiButton.Accept)
            {
                _cryoSystem.CryoStoreBody(body, _cryopod);
            }
            else
            {
                _cryoSystem.EjectBody(_cryopod, body: body);
            }
        }

        Close();
    }
}
