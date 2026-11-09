using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using JetBrains.Annotations;

namespace Content.Server._NF.Auth;

public sealed class MiniAuthManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _http = new();

    /// <summary>
    /// Frontier function to ping a server and check to see if the given player is currently connected to the given server.
    /// Servers using this function must share an admin_api token as defined in their respective server_config.toml
    /// </summary>
    /// <param name="address">The address of the server to ping.</param>
    /// <param name="player">the GUID of the player to check for connection.</param>
    /// <returns>True if the response from the server is successful and the player is connected. False in any case of error, timeout, or failure.</returns>
    public async Task<bool> IsPlayerConnected(string address, Guid player)
    {
        var connected = false;
        var statusAddress = "http://" + address + "/admin/info";

        var cancel = new CancellationToken();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        linkedToken.CancelAfter(TimeSpan.FromSeconds(10));

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SS14Token", _cfg.GetCVar(CCVars.AdminApiToken));

        //We need to do a try catch here because theres essentially no way to guarantee our json response is proper.
        //Throughout all of this, we want it to fail to deny, not fail to allow, so if any step of our auth goes wrong,
        //people can still connect.
        try
        {
            var status = await _http.GetFromJsonAsync<InfoResponse>(statusAddress, linkedToken.Token);

            foreach (var connectedPlayer in status!.Players)
            {
                if (connectedPlayer.UserId == player)
                {
                    connected = true;
                    break;
                }
            }
        }
        catch (Exception)
        {
        }
        return connected;
    }

    /// <summary>
    /// Record used to send the response for the info endpoint.
    /// Frontier - This is a direct copy of ServerAPI.InfoResponse to match the json format. they kept it private so i just copied it
    /// </summary>
    [UsedImplicitly]
    private sealed record InfoResponse
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
