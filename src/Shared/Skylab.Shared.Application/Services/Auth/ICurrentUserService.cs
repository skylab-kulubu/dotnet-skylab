namespace Skylab.Shared.Application.Services;

public interface ICurrentUserService
{
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);
}
