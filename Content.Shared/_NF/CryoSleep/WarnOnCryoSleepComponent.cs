using Content.Shared.Eui;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.CryoSleep;

//TODO: Better documentation

//Warns the user if they enter cryosleep with this in their inventory
[RegisterComponent]
[NetworkedComponent]
public sealed partial class WarnOnCryoSleepComponent : EuiMessageBase
{

}
