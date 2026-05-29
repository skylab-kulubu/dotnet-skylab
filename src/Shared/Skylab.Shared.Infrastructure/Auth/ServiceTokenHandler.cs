using System.Net.Http.Headers;
using Skylab.Shared.Application.Services;

namespace Skylab.Shared.Infrastructure.Auth;

public class ServiceTokenHandler : DelegatingHandler
{
    private readonly IServiceTokenProvider _tokenProvider;

    public ServiceTokenHandler(IServiceTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}