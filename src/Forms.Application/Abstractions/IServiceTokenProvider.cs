namespace Skylab.Forms.Application.Abstractions;

public interface IServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}
