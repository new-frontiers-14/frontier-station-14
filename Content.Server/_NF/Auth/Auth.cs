using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
//using System.IO;
using System.Net.Http.Headers;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;

namespace Content.Server._NF.Auth;

public sealed class MiniAuthManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private readonly HttpClient _http = new();
    private readonly ISawmill _sawmill = default!;

    public async Task<bool> IsPlayerConnected(string address, Guid player)
    {
        var connected = false;
        var statusAddress = "http://" + address + "/admin/info";

        var cancel = new CancellationToken();

        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        linkedToken.CancelAfter(TimeSpan.FromSeconds(10));

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SS14Token", _cfg.GetCVar(CCVars.AdminApiToken));
        using var response = await _http.GetAsync(statusAddress, linkedToken.Token);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return connected;

        if (response.IsSuccessStatusCode)
        {
            response.StatusCode.ToString();
            var status = await response.Content.ReadFromJsonAsync<InfoResponse>(linkedToken.Token);
            foreach (var connectedPlayer in status!.Players)
            {
                if (connectedPlayer.UserId == player)
                    connected = true;
            }
        }
        else
        {
            _sawmill.Error("Auth server returned bad response {StatusCode}!", response.StatusCode);
        }
        //var status = await _http.GetFromJsonAsync<ServerApi.InfoResponse>(statusAddress, linkedToken.Token);
        return connected;
    }
    /// <summary>
    /// Record used to send the response for the info endpoint.
    /// </summary>
    [UsedImplicitly]
    private sealed record InfoResponse //frontier - public to maybe reuse
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
