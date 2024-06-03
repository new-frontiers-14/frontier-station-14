using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
//using System.IO;
using System.Net.Http.Headers;
using Content.Server.Administration;
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

        var status = await _http.GetFromJsonAsync<ServerApi.InfoResponse>(statusAddress, linkedToken.Token);
        if (status == null)
            return connected;

        foreach (var connectedPlayer in status.Players)
        {
            if (connectedPlayer.UserId == player)
                connected = true;
        }

        return connected;
    }
}
