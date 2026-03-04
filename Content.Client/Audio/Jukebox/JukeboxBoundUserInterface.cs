using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Audio.Jukebox;

public sealed class JukeboxBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private readonly SharedJukeboxSystem _sharedJukeboxSystem = default!; // wizden#42210

    [ViewVariables]
    private JukeboxMenu? _menu;

    public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sharedJukeboxSystem = EntMan.System<SharedJukeboxSystem>(); // wizden#42210
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<JukeboxMenu>();

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new JukeboxPlayingMessage());
            }
            else
            {
                SendMessage(new JukeboxPauseMessage());
            }
        };

        _menu.OnStopPressed += () =>
        {
            SendMessage(new JukeboxStopMessage());
        };

        // wizden#42210
        _menu.OnRepeatToggled += args =>
        {
            SendMessage(new JukeboxRepeatMessage(args));
        };

        _menu.OnShuffleToggled += args =>
        {
            SendMessage(new JukeboxShuffleMessage(args));
        };
        // End wizden#42210

        _menu.SetTime += SetTime;
        _menu.TrackQueueAction += track => // wizden#42210
        {
            SendMessage(new JukeboxQueueTrackMessage(track)); // wizden#42210
        };
        _menu.QueueDeleteAction += index => SendMessage(new JukeboxDeleteRequestMessage(index)); // wizden#42210
        _menu.QueueMoveUpAction += index => SendMessage(new JukeboxMoveRequestMessage(index, -1)); // wizden#42210
        _menu.QueueMoveDownAction += index => SendMessage(new JukeboxMoveRequestMessage(index, 1)); // wizden#42210

        PopulateMusic();
        Reload();
    }

    /// <summary>
    /// Reloads the attached menu if it exists.
    /// </summary>
    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
            return;

        _menu.SetAudioStream(jukebox.AudioStream);

        if (_protoManager.Resolve(jukebox.SelectedSongId, out var songProto)) // wizden#42210
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(songProto.Path.Path.ToString());
            _menu.SetSelectedSong(songProto, (float)length.TotalSeconds); // wizden#42210
        }
        else
        {
            _menu.SetSelectedSong(null, 0f); // wizden#42210
        }

        _menu.PopulateQueueList(jukebox.Queue); // wizden#42210
        _menu.UpdateButtons(jukebox.RepeatTracks, jukebox.ShuffleTracks); // wizden#42210
    }

    public void PopulateMusic()
    {
        if (!EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox)) // wizden#42210
            return;

        _menu?.UpdateAvailableTracks(_sharedJukeboxSystem.GetAvailableTracks((Owner, jukebox))); // wizden#42210
        _menu?.PopulateTracklist(); // wizden#42210
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        // You may be wondering, what the fuck is this
        // Well we want to be able to predict the playback slider change, of which there are many ways to do it
        // We can't just use SendPredictedMessage because it will reset every tick and audio updates every frame
        // so it will go BRRRRT
        // Using ping gets us close enough that it SHOULD, MOST OF THE TIME, fall within the 0.1 second tolerance
        // that's still on engine so our playback position never gets corrected.
        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new JukeboxSetTimeMessage(sentTime));
    }
}

