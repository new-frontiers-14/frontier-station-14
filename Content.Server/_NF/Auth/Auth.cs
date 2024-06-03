using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
//using System.IO;
using System.Net.Http.Headers;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._NF.Auth;

public sealed class MiniAuthManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly HttpClient _http = new();

    public async Task<bool> IsPlayerConnected(string address, Guid player)
    {
        var connected = false;
        var statusAddress = "http://" + address + "/admin/info";

        var cancel = new CancellationToken();

        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        linkedToken.CancelAfter(TimeSpan.FromSeconds(10));

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SS14Token", _cfg.GetCVar(CCVars.AdminApiToken));

        var status = await _http.GetFromJsonAsync<InfoResponse>(statusAddress, linkedToken.Token);
        if (status == null)
            return connected;

        foreach (var connectedPlayer in status.Players)
        {
            if (connectedPlayer.UserId == player)
                connected = true;
        }

        return connected;
    }

    /// <summary>
    /// Record used to recieve the response for the info endpoint.
    /// </summary>
    private sealed class InfoResponse
    {
        public required int RoundId { get; init; }
        public required List<Player> Players { get; init; }
        public required List<string> GameRules { get; init; }
        public required string? GamePreset { get; init; }
        public required MapInfo? Map { get; init; }
        public required string? MOTD { get; init; }
        public required Dictionary<string, object> PanicBunker { get; init; }

        public sealed class Player
        {
            public required Guid UserId { get; init; }
            public required string Name { get; init; }
            public required bool IsAdmin { get; init; }
            public required bool IsDeadminned { get; init; }
        }

        public sealed class MapInfo
        {
            public required string Id { get; init; }
            public required string Name { get; init; }
        }
    }
}
