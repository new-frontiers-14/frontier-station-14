// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.Silicons.StationAi;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Chat.Systems;


/// <summary>
///     AnnounceTTSSystem is responsible for TTS announcements, such as through the Station AI.
/// </summary>
public sealed partial class AnnounceTtsSystem : EntitySystem
{
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly IEntityManager _entityManager = default!;

	private Queue<TtsMessageEntry> _ttsQueue = default!;
	private CancellationTokenSource _source = default!;
	private const int DefaultDurationMs = 100;

	public override void Initialize()
    {
        base.Initialize();

		_ttsQueue = [];
		_source = new CancellationTokenSource();

        Timer.Spawn(DefaultDurationMs, PopTtsMessage, _source.Token);
	}

	private void PopTtsMessage()
	{
        var filepath = "";
        bool played;
        StationAiCoreComponent? ai = null;

		if (_ttsQueue.Count > 0)
		{
			var dequeued = _ttsQueue.Dequeue();
            var filename = dequeued.Filename;
            ai = dequeued.Source;

			filepath = (filename.StartsWith("/") ? filename : ("/Audio/Announcements/VoxFem/"+filename+".ogg"));
			try
			{
				_audio.PlayGlobal(filepath, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
				played = true;
			}
			catch (Exception e)
			{
				played = false;
			}
		}
		else
		{
			played = false;
		}

		if (played && ai != null)
			Timer.Spawn((int) _audio.GetAudioLength(filepath).TotalMilliseconds + ai.TtsBufferBetweenWordsMs, PopTtsMessage, _source.Token);
		else
			Timer.Spawn(DefaultDurationMs, PopTtsMessage, _source.Token);
	}

	public void QueueTtsMessage(StationAiCoreComponent? aiCore, List<string> filenames, string? alertSound = null)
	{
		if (alertSound != null)
        {
            _ttsQueue.Enqueue(new TtsMessageEntry
            {
                Source = aiCore ?? null,
                Filename = alertSound,
            });
        }

        foreach (var filename in filenames)
		{
			_ttsQueue.Enqueue(new TtsMessageEntry
            {
                Source = aiCore ?? null,
                Filename = filename,
            });
		}

	}

	public bool CanTTS(EntityUid user)
    {
        return false; // disable ai tts for now
        //return _entityManager.HasComponent<StationAiOverlayComponent>(user);
    }

	public static List<string> PrepareTtsMessage(string msg)
	{
		string lowered = msg.ToLower();
		lowered = lowered.Replace("&", " and ");
		lowered = lowered.Replace("+", " plus ");
		lowered = lowered.Replace("/", " or ");
		lowered = lowered.Replace(".", " . ");
		lowered = lowered.Replace(",", " , ");
		lowered = lowered.Replace("%", " percent ");
		var splitted = lowered.Split(' ', '\n', '-', ';', '+', '/', '?', '!', '&', '*', '(', ')', '%', '$', '[', ']');
		return new List<string>(splitted);
	}
}

internal record TtsMessageEntry
{
    internal required StationAiCoreComponent? Source;
    internal required string Filename;
}
