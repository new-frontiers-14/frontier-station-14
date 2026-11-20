using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;

namespace Content.Server._UNEON.Subscriptions;

/// <summary>
/// Queries our VPS only. No Discord token or direct Discord connection exists in the game server.
/// All world/session/cache access happens on the simulation thread; HTTP completions enqueue work.
/// This foundation exposes rights for future perk systems, but does not grant gameplay rewards yet.
/// </summary>
public sealed class UneonSubscriptionsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IResourceManager _resources = default!;

    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(15) };
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentQueue<Action> _completions = new();
    private readonly Dictionary<string, UneonSubscriptionEntry> _cache = new();
    private readonly HashSet<string> _busy = new();
    private readonly Dictionary<string, DateTimeOffset> _lastCommand = new();
    private DateTimeOffset _nextRefresh;
    private string? _cachePath;
    private ISawmill _log = default!;

    private sealed class RightsResponse
    {
        public RightsResponse() { }
        [JsonPropertyName("user_id")] public string UserId { get; set; } = "";
        [JsonPropertyName("linked")] public bool Linked { get; set; }
        [JsonPropertyName("tier")] public int Tier { get; set; }
        [JsonPropertyName("permissions")] public string[] Permissions { get; set; } = Array.Empty<string>();
        [JsonPropertyName("verified_at")] public long VerifiedAt { get; set; }
    }

    private sealed class LinkResponse
    {
        public LinkResponse() { }
        [JsonPropertyName("linked")] public bool Linked { get; set; }
    }

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("uneon.subscriptions");
        _console.RegisterCommand("uneon_subscription", Loc.GetString("uneon-sub-help"),
            "uneon_subscription [link CODE|unlink CONFIRM]", Command);
        if (_resources.UserData.RootDir is { } root)
        {
            _cachePath = Path.Combine(root, "uneon-subscriptions.json");
            try
            {
                if (File.Exists(_cachePath))
                {
                    var entries = JsonSerializer.Deserialize<Dictionary<string, UneonSubscriptionEntry>>(File.ReadAllText(_cachePath));
                    if (entries != null)
                    {
                        foreach (var (user, entry) in entries)
                        {
                            if (Guid.TryParse(user, out _) && entry != null && entry.Permissions != null && entry.Tier >= 0)
                                _cache[user] = entry;
                        }
                    }
                }
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
            {
                _log.Warning("Subscription cache could not be read; rights must be verified online.");
            }
        }
    }

    public override void Shutdown()
    {
        _console.UnregisterCommand("uneon_subscription");
        _shutdown.Cancel();
        _http.Dispose();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        while (_completions.TryDequeue(out var completion))
            completion();

        if (!_cfg.GetCVar(CCVars.UneonSubscriptionsEnabled) || DateTimeOffset.UtcNow < _nextRefresh)
            return;
        _nextRefresh = DateTimeOffset.UtcNow.AddMinutes(2);
        foreach (var session in _players.Sessions)
            StartRequest(session.UserId.ToString(), "status", null);
    }

    /// <summary>Checks the bounded persistent cache. Call on the simulation thread.</summary>
    public bool HasPermission(NetUserId user, string permission)
    {
        return _cfg.GetCVar(CCVars.UneonSubscriptionsEnabled)
               && TryGetValidEntry(user.ToString(), out var entry)
               && entry!.Grants(permission, DateTimeOffset.UtcNow,
                   _cfg.GetCVar(CCVars.UneonSubscriptionsGraceMinutes));
    }

    /// <summary>Returns zero for unlinked, expired or disabled subscriptions.</summary>
    public int GetTier(NetUserId user)
    {
        return _cfg.GetCVar(CCVars.UneonSubscriptionsEnabled) &&
               TryGetValidEntry(user.ToString(), out var entry) && entry!.Linked
            ? Math.Max(0, entry.Tier) : 0;
    }

    private bool TryGetValidEntry(string user, out UneonSubscriptionEntry? entry)
    {
        if (!_cache.TryGetValue(user, out entry))
            return false;
        return entry.IsValid(DateTimeOffset.UtcNow, _cfg.GetCVar(CCVars.UneonSubscriptionsGraceMinutes));
    }

    [AnyCommand]
    private void Command(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("uneon-sub-player-only"));
            return;
        }
        if (!_cfg.GetCVar(CCVars.UneonSubscriptionsEnabled))
        {
            shell.WriteLine(Loc.GetString("uneon-sub-disabled"));
            return;
        }
        var action = args.Length == 0 ? "status" : args[0];
        if (action == "link" && (args.Length != 2 || args[1].Length != 16 || args[1].Any(c => !char.IsAsciiHexDigit(c))) ||
            action == "unlink" && (args.Length != 2 || args[1] != "CONFIRM") ||
            action == "status" && args.Length > 1 || action is not ("status" or "link" or "unlink"))
        {
            shell.WriteLine(Loc.GetString("uneon-sub-help"));
            return;
        }
        var user = player.UserId.ToString();
        if (_lastCommand.TryGetValue(user, out var last) && DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(10))
        {
            shell.WriteLine(Loc.GetString("uneon-sub-wait"));
            return;
        }
        _lastCommand[user] = DateTimeOffset.UtcNow;
        if (!StartRequest(user, action, shell, action == "link" ? args[1] : null))
            shell.WriteLine(Loc.GetString("uneon-sub-wait"));
    }

    private string SubscriptionName(int tier)
    {
        return Loc.GetString(tier switch
        {
            0 => "uneon-sub-tier-none",
            1 => "uneon-sub-tier-adventurer",
            2 => "uneon-sub-tier-cosmopioneer",
            3 => "uneon-sub-tier-shuttle-captain",
            4 => "uneon-sub-tier-sector-baron",
            5 => "uneon-sub-tier-frontier-legend",
            _ => "uneon-sub-tier-unknown"
        });
    }

    private bool StartRequest(string user, string action, IConsoleShell? shell, string? code = null)
    {
        if (!_busy.Add(user))
            return false;
        var url = _cfg.GetCVar(CCVars.UneonSubscriptionsUrl).TrimEnd('/');
        var key = _cfg.GetCVar(CCVars.UneonSubscriptionsKey);
        // Plain HTTP is allowed only through an explicit local tunnel/development endpoint.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || key.Length < 32)
        {
            _busy.Remove(user);
            shell?.WriteError(Loc.GetString("uneon-sub-config-error"));
            return true;
        }
        _ = RequestAsync(user, action, shell, url, key, code);
        return true;
    }

    private async Task RequestAsync(string user, string action, IConsoleShell? shell, string url, string key, string? code)
    {
        try
        {
            using var request = new HttpRequestMessage(action == "status" ? HttpMethod.Get : HttpMethod.Post,
                action == "status" ? $"{url}/v1/entitlements/{user}" : $"{url}/v1/{action}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            if (action != "status")
                // JsonContent streams chunked HTTP/1.1 by default. Use a known length for simple API servers/proxies.
                request.Content = new StringContent(JsonSerializer.Serialize(new { user_id = user, code }),
                    Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, _shutdown.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (action == "status")
            {
                var data = await response.Content.ReadFromJsonAsync<RightsResponse>(cancellationToken: _shutdown.Token).ConfigureAwait(false);
                if (data == null || data.UserId != user || data.Tier < 0 || data.Permissions == null ||
                    data.Permissions.Length > 256 || data.Permissions.Any(p => string.IsNullOrEmpty(p) || p.Length > 80) ||
                    Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.VerifiedAt) > 300 ||
                    (!data.Linked || data.Tier == 0) && data.Permissions.Length != 0)
                    throw new JsonException("Invalid rights response");
                var entry = new UneonSubscriptionEntry { Linked = data.Linked, Tier = data.Tier,
                    Permissions = data.Permissions, ConfirmedAt = DateTimeOffset.UtcNow };
                _completions.Enqueue(() =>
                {
                    _cache[user] = entry; // An explicit tier 0 revokes old rights immediately.
                    SaveCache();
                    shell?.WriteLine(Loc.GetString("uneon-sub-status", ("linked", Loc.GetString(entry.Linked ? "uneon-sub-discord-linked" : "uneon-sub-discord-not-linked")),
                        ("tier", entry.Tier), ("name", SubscriptionName(entry.Tier))));
                });
            }
            else if (action == "link")
            {
                var data = await response.Content.ReadFromJsonAsync<LinkResponse>(cancellationToken: _shutdown.Token).ConfigureAwait(false);
                if (data == null || !data.Linked)
                    throw new JsonException("Invalid link confirmation");
                _completions.Enqueue(() =>
                {
                    _cache.Remove(user);
                    SaveCache();
                    shell?.WriteLine(Loc.GetString("uneon-sub-linked"));
                });
            }
            else
            {
                _completions.Enqueue(() =>
                {
                    _cache.Remove(user);
                    SaveCache();
                    shell?.WriteLine(Loc.GetString("uneon-sub-unlinked"));
                });
            }
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException)
        {
            if (!_shutdown.IsCancellationRequested)
                _completions.Enqueue(() =>
                {
                    // Never log URLs, tokens or exception payloads from remote services.
                    _log.Warning("Subscription request failed; using bounded last-confirmed rights where available.");
                    if (shell == null)
                        return;
                    var rejectedCode = action == "link" && e is HttpRequestException
                        { StatusCode: System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest };
                    shell.WriteLine(Loc.GetString(rejectedCode ? "uneon-sub-code-invalid" : "uneon-sub-unavailable"));
                    if (action == "status" && TryGetValidEntry(user, out var cached))
                        shell.WriteLine(Loc.GetString("uneon-sub-cached", ("tier", cached!.Tier), ("name", SubscriptionName(cached.Tier)),
                            ("time", cached.ConfirmedAt.ToString("u"))));
                });
        }
        finally
        {
            _completions.Enqueue(() => _busy.Remove(user));
        }
    }

    private void SaveCache()
    {
        if (_cachePath == null)
            return;
        try
        {
            File.WriteAllText(_cachePath + ".tmp", JsonSerializer.Serialize(_cache));
            File.Move(_cachePath + ".tmp", _cachePath, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _log.Warning("Subscription cache could not be saved.");
        }
    }
}
