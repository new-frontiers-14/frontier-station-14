using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking.Presets;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        public const float PresetFailedCooldownIncrease = 30f;

        /// <summary>
        /// The selected preset that will be used at the start of the next round.
        /// </summary>
        public GamePresetPrototype? Preset { get; private set; }

        /// <summary>
        /// The preset that's currently active.
        /// </summary>
        public GamePresetPrototype? CurrentPreset { get; private set; }

        private bool StartPreset(ICommonSession[] origReadyPlayers, bool force)
        {
            var startAttempt = new RoundStartAttemptEvent(origReadyPlayers, force);
            RaiseLocalEvent(startAttempt);

            if (!startAttempt.Cancelled)
                return true;

            var presetTitle = CurrentPreset != null ? Loc.GetString(CurrentPreset.ModeTitle) : string.Empty;

            void FailedPresetRestart()
            {
                SendServerMessage(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-restart",
                    ("failedGameMode", presetTitle)));
                RestartRound();
                DelayStart(TimeSpan.FromSeconds(PresetFailedCooldownIncrease));
            }

            if (_configurationManager.GetCVar(CCVars.GameLobbyFallbackEnabled))
            {
                var fallbackPresets = _configurationManager.GetCVar(CCVars.GameLobbyFallbackPreset).Split(",");
                var startFailed = true;

                foreach (var preset in fallbackPresets)
                {
                    ClearGameRules();
                    SetGamePreset(preset);
                    AddGamePresetRules();
                    StartGamePresetRules();

                    startAttempt.Uncancel();
                    RaiseLocalEvent(startAttempt);

                    if (!startAttempt.Cancelled)
                    {
                        _chatManager.SendAdminAnnouncement(
                            Loc.GetString("game-ticker-start-round-cannot-start-game-mode-fallback",
                                ("failedGameMode", presetTitle),
                                ("fallbackMode", Loc.GetString(preset))));
                        RefreshLateJoinAllowed();
                        startFailed = false;
                        break;
                    }
                }

                if (startFailed)
                {
                    FailedPresetRestart();
                    return false;
                }
            }

            else
            {
                FailedPresetRestart();
                return false;
            }

            return true;
        }

        private void InitializeGamePreset()
        {
            SetGamePreset(LobbyEnabled ? _configurationManager.GetCVar(CCVars.GameLobbyDefaultPreset) : "sandbox");
        }

        public void SetGamePreset(GamePresetPrototype preset, bool force = false)
        {
            // Do nothing if this game ticker is a dummy!
            if (DummyTicker)
                return;

            Preset = preset;
            UpdateInfoText();
            ValidateMap();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetGamePreset(string preset, bool force = false)
        {
            var proto = FindGamePreset(preset);
            if(proto != null)
                SetGamePreset(proto, force);
        }

        public GamePresetPrototype? FindGamePreset(string preset)
        {
            if (_prototypeManager.TryIndex(preset, out GamePresetPrototype? presetProto))
                return presetProto;

            foreach (var proto in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
            {
                foreach (var alias in proto.Alias)
                {
                    if (preset.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                        return proto;
                }
            }

            return null;
        }

        public bool TryFindGamePreset(string preset, [NotNullWhen(true)] out GamePresetPrototype? prototype)
        {
            prototype = FindGamePreset(preset);

            return prototype != null;
        }

        public bool IsMapEligible(GameMapPrototype map)
        {
            if (Preset == null)
                return true;

            if (Preset.MapPool == null || !_prototypeManager.TryIndex<GameMapPoolPrototype>(Preset.MapPool, out var pool))
                return true;

            return pool.Maps.Contains(map.ID);
        }

        private void ValidateMap()
        {
            if (Preset == null || _gameMapManager.GetSelectedMap() is not { } map)
                return;

            if (Preset.MapPool == null ||
                !_prototypeManager.TryIndex<GameMapPoolPrototype>(Preset.MapPool, out var pool))
                return;

            if (pool.Maps.Contains(map.ID))
                return;

            _gameMapManager.SelectMapRandom();
        }

        [PublicAPI]
        private bool AddGamePresetRules()
        {
            if (DummyTicker || Preset == null)
                return false;

            CurrentPreset = Preset;
            foreach (var rule in Preset.Rules)
            {
                AddGameRule(rule);
            }

            return true;
        }

        private void StartGamePresetRules()
        {
            // May be touched by the preset during init.
            var rules = new List<EntityUid>(GetAddedGameRules());
            foreach (var rule in rules)
            {
                StartGameRule(rule);
            }
        }

        public bool OnGhostAttempt(EntityUid mindId, bool canReturnGlobal, bool viaCommand = false, MindComponent? mind = null)
        {
            if (!Resolve(mindId, ref mind))
                return false;

            var playerEntity = mind.CurrentEntity;

            if (playerEntity != null && viaCommand)
                _adminLogger.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} is attempting to ghost via command");

            var handleEv = new GhostAttemptHandleEvent(mind, canReturnGlobal);
            RaiseLocalEvent(handleEv);

            // Something else has handled the ghost attempt for us! We return its result.
            if (handleEv.Handled)
                return handleEv.Result;

            if (mind.PreventGhosting)
            {
                if (mind.Session != null) // Logging is suppressed to prevent spam from ghost attempts caused by movement attempts
                {
                    _chatManager.DispatchServerMessage(mind.Session, Loc.GetString("comp-mind-ghosting-prevented"),
                        true);
                }

                return false;
            }

            if (HasComp<GhostComponent>(playerEntity))
                return false;

            if (mind.VisitingEntity != default)
            {
                _mind.UnVisit(mindId, mind: mind);
            }

            var position = Exists(playerEntity)
                ? Transform(playerEntity.Value).Coordinates
                : GetObserverSpawnPoint();

            if (position == default)
                return false;

            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            //   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && _mind.IsCharacterDeadPhysically(mind);

            if (canReturnGlobal && TryComp(playerEntity, out MobStateComponent? mobState))
            {
                if (_mobState.IsCritical(playerEntity.Value, mobState))
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply
                    DamageSpecifier damage = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), 200);
                    _damageable.TryChangeDamage(playerEntity, damage, true);
                }
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var coords = _transform.GetMoverCoordinates(position, xformQuery);

            var ghost = Spawn("MobObserver", coords);

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                _metaData.SetEntityName(ghost, mind.CharacterName);
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                _metaData.SetEntityName(ghost, mind.Session.Name);

            var ghostComponent = Comp<GhostComponent>(ghost);

            if (mind.TimeOfDeath.HasValue)
            {
                _ghost.SetTimeOfDeath(ghost, mind.TimeOfDeath!.Value, ghostComponent);
            }

            if (playerEntity != null)
                _adminLogger.Add(LogType.Mind, $"{EntityManager.ToPrettyString(playerEntity.Value):player} ghosted{(!canReturn ? " (non-returnable)" : "")}");

            _ghost.SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                _mind.Visit(mindId, ghost, mind);
            else
                _mind.TransferTo(mindId, ghost, mind: mind);
            return true;
        }

        private void IncrementRoundNumber()
        {
            var playerIds = _playerGameStatuses.Keys.Select(player => player.UserId).ToArray();
            var serverName = _configurationManager.GetCVar(CCVars.AdminLogsServerName);

            // TODO FIXME AAAAAAAAAAAAAAAAAAAH THIS IS BROKEN
            // Task.Run as a terrible dirty workaround to avoid synchronization context deadlock from .Result here.
            // This whole setup logic should be made asynchronous so we can properly wait on the DB AAAAAAAAAAAAAH
            var task = Task.Run(async () =>
            {
                var server = await _db.AddOrGetServer(serverName);
                return await _db.AddNewRound(server, playerIds);
            });

            _taskManager.BlockWaitOnTask(task);
            RoundId = task.GetAwaiter().GetResult();
        }
    }

    public sealed class GhostAttemptHandleEvent : HandledEntityEventArgs
    {
        public MindComponent Mind { get; }
        public bool CanReturnGlobal { get; }
        public bool Result { get; set; }

        public GhostAttemptHandleEvent(MindComponent mind, bool canReturnGlobal)
        {
            Mind = mind;
            CanReturnGlobal = canReturnGlobal;
        }
    }
}
