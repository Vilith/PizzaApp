using System.Net;
using System.Net.Http.Json;
using PizzaApp.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_uses_server_profile_refreshes_once_and_logout_clears_credentials()
    {
        var backend = new Backend { ExpiresIn = 1 };
        var auth = new AuthService(backend);
        await auth.SignInAsync("anna@example.test", "password");
        Assert.False(auth.User!.IsAdmin);
        var tokens = await Task.WhenAll(auth.GetAccessTokenAsync(), auth.GetAccessTokenAsync());
        Assert.All(tokens, token => Assert.Equal("refreshed-token", token));
        Assert.Equal(1, backend.RefreshCalls);
        await auth.SignOutAsync();
        Assert.True(backend.LoggedOut);
        Assert.Null(auth.User);
        Assert.Null(await auth.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Rejected_login_or_unapproved_profile_never_creates_a_session()
    {
        var backend = new Backend { RejectPassword = true };
        var auth = new AuthService(backend);
        await Assert.ThrowsAsync<AuthException>(() => auth.SignInAsync("anna@example.test", "wrong"));
        Assert.Null(auth.User);
        backend.RejectPassword = false; backend.RejectProfile = true;
        await Assert.ThrowsAsync<AuthException>(() => auth.SignInAsync("anna@example.test", "password"));
        Assert.Null(auth.User);
        Assert.Null(await auth.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Failed_refresh_requires_login_and_failed_remote_logout_still_clears_session()
    {
        var backend = new Backend { ExpiresIn = 1 };
        var auth = new AuthService(backend);
        await auth.SignInAsync("anna@example.test", "password");
        backend.RejectRefresh = true;
        await Assert.ThrowsAsync<AuthException>(() => auth.GetAccessTokenAsync());
        Assert.Null(auth.User);
        backend.ExpiresIn = 3600;
        await auth.SignInAsync("anna@example.test", "password");
        backend.FailLogout = true;
        await Assert.ThrowsAsync<HttpRequestException>(() => auth.SignOutAsync());
        Assert.Null(auth.User);
        Assert.Null(await auth.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Clearing_session_during_refresh_does_not_resurrect_it()
    {
        var backend = new Backend { ExpiresIn = 1 };
        var auth = new AuthService(backend);
        await auth.SignInAsync("anna@example.test", "password");
        backend.OnRefresh = auth.ClearSession;
        Assert.Null(await auth.GetAccessTokenAsync());
        Assert.Null(auth.User);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    public async Task Api_handler_sends_token_and_handles_access_errors(HttpStatusCode status, bool clearsSession)
    {
        var auth = new FakeAuth();
        using var client = new HttpClient(new AuthenticatedApiHandler(auth) { InnerHandler = new ResponseHandler(status) });
        await Assert.ThrowsAsync<AuthException>(() => client.GetAsync("https://api.example.test/api/restaurants"));
        Assert.Equal(clearsSession, auth.User == null);
    }

    private sealed class ResponseHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class Backend : HttpMessageHandler, IHttpClientFactory
    {
        public int ExpiresIn = 3600;
        public int RefreshCalls;
        public bool RejectPassword, RejectProfile, RejectRefresh, FailLogout, LoggedOut;
        public Action? OnRefresh;
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false) { BaseAddress = new("https://api.example.test/") };
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path == "/api/auth/config") return Json(new AuthConfiguration("https://auth.example.test", "sb_publishable_test"));
            if (path == "/api/auth/me")
            {
                Assert.NotNull(request.Headers.Authorization);
                return RejectProfile ? Status(HttpStatusCode.Unauthorized) : Json(new SignedInUser(new TestUser().Id, "anna@example.test", false));
            }
            Assert.Equal("sb_publishable_test", Assert.Single(request.Headers.GetValues("apikey")));
            if (path.Contains("grant_type=password"))
                return RejectPassword ? Status(HttpStatusCode.BadRequest) : Json(new { access_token = "login-token", refresh_token = "refresh-secret", expires_in = ExpiresIn });
            if (path.Contains("grant_type=refresh_token"))
            {
                RefreshCalls++; OnRefresh?.Invoke();
                return RejectRefresh ? Status(HttpStatusCode.BadRequest) : Json(new { access_token = "refreshed-token", refresh_token = "rotated-secret", expires_in = 3600 });
            }
            if (path == "/auth/v1/logout?scope=local")
            {
                LoggedOut = true;
                return Status(FailLogout ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.NoContent);
            }
            throw new InvalidOperationException("Unexpected test request: " + path);
        }
        private static Task<HttpResponseMessage> Json(object value) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(value) });
        private static Task<HttpResponseMessage> Status(HttpStatusCode code) => Task.FromResult(new HttpResponseMessage(code));
    }
}
