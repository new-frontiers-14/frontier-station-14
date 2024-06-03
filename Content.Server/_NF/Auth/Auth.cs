using System.Collections.Immutable;
using System.Net;
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

        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Error("Auth server returned bad response {StatusCode}!", response.StatusCode);
            return connected;
        }
        _sawmill.Info(response.StatusCode.ToString());
        using var status = await response.Content.ReadFromJsonAsync<ServerApi.InfoResponse>(linkedToken.Token);
        //var status = await _http.GetFromJsonAsync<ServerApi.InfoResponse>(statusAddress, linkedToken.Token);
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
