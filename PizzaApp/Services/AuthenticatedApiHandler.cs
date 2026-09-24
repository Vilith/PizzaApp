using System.Net;
using System.Net.Http.Headers;

namespace PizzaApp.Services;

public sealed class AuthenticatedApiHandler(IAuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await auth.GetAccessTokenAsync();
        if (token == null) throw new AuthException("Logga in för att fortsätta.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            auth.ClearSession();
            throw new AuthException("Din inloggning är inte längre giltig. Logga in igen.");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            response.Dispose();
            throw new AuthException("Du saknar behörighet för den här åtgärden.");
        }
        return response;
    }
}
