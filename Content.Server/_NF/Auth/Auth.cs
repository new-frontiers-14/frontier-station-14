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
        var statusAddress = "http://" + address.Split("//")[1] + "/admin/info";

        var cancel = new CancellationToken();

        try
        {
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                linkedToken.CancelAfter(TimeSpan.FromSeconds(10));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SS14Token", _cfg.GetCVar(CCVars.AdminApiToken));
                var status = await _http.GetFromJsonAsync<ServerApi.InfoResponse>(statusAddress, linkedToken.Token)
                            ?? throw new NotImplementedException();
                foreach (var connectedPlayer in status.Players)
                {
                    if (connectedPlayer.UserId == player)
                        return true;
                }
            }

            cancel.ThrowIfCancellationRequested();
        }
        catch
        {
            return false;
        }

        return false;
    }
}
