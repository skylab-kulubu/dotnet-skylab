namespace Skylab.Shared.Application.Services;

public interface ICurrentUserService
{
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);

    Task<bool> HasRoleAsync(string role, string? client = null, CancellationToken cancellationToken = default);
}
