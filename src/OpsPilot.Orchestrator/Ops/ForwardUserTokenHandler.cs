using System.Net.Http.Headers;

namespace OpsPilot.Orchestrator.Ops;

public sealed class ForwardUserTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header) && AuthenticationHeaderValue.TryParse(header, out var value))
            request.Headers.Authorization = value;

        return base.SendAsync(request, cancellationToken);
    }
}
