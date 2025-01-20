using Robust.Shared.GameStates;


namespace Content.Server.Silicon.Components.Borgs;

[RegisterComponent]
public sealed partial class BorgJetpackComponent : Component
{
    public EntityUid? JetpackUid = null;
}
