namespace Skylab.Shared.Application.Services;

public interface IServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}