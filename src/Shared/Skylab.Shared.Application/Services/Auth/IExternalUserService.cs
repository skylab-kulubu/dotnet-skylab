using Skylab.Shared.Application.Contracts.Auth;

namespace Skylab.Shared.Application.Services;

public interface IExternalUserService
{
    Task<UserContract?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<UserContract>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}