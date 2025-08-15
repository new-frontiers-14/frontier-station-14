using Robust.Client.Audio;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Harmony.JoinQueue;

public sealed class QueueState : State
{
    [Dependency] private readonly IClientJoinQueueManager _joinQueueManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    protected override Type? LinkedScreenType { get; } = typeof(QueueGui);
    public QueueGui? Queue;

    private static readonly SoundSpecifier JoinSoundPath = new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg");

    protected override void Startup()
    {
        if (_userInterfaceManager.ActiveScreen == null)
        {
            return;
        }

        Queue = (QueueGui) _userInterfaceManager.ActiveScreen;

        Queue.QuitButton.OnPressed += OnQuitButtonPressed;

        _joinQueueManager.QueueStateUpdated += OnQueueStateUpdated;
        OnQueueStateUpdated(); // Update the current state, even if it might be incorrect.
    }

    protected override void Shutdown()
    {
        _joinQueueManager.QueueStateUpdated -= OnQueueStateUpdated;

        if (_entityManager.TrySystem<AudioSystem>(out var audio))
            audio.PlayGlobal(JoinSoundPath, Filter.Local(), false);
    }

    private void OnQuitButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _netManager.ClientDisconnect(_loc.GetString("queue-disconnect-reason"));
    }

    private void OnQueueStateUpdated()
    {
        Queue?.UpdateInfo(_joinQueueManager.PlayerInQueueCount, _joinQueueManager.CurrentPosition);
    }
}
