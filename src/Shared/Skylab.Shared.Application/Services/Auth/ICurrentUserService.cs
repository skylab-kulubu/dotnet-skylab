namespace Skylab.Shared.Application.Services;

public interface ICurrentUserService
{
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// client null ise realm_access.roles, değilse resource_access.{client}.roles içinde arar.
    /// </summary>
    Task<bool> HasRoleAsync(string role, string? client = null, CancellationToken cancellationToken = default);
}
