using Skylab.Forms.Application.Contracts.Identity;

namespace Skylab.Forms.Application.Abstractions;

public interface IExternalUserService
{
    Task<UserContract?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<UserContract>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
