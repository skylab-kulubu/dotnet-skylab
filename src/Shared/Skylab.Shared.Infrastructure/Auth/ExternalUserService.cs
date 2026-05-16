using System.Net.Http.Json;
using System.Text.Json;
using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Application.Contracts.Auth;
using Skylab.Shared.Application.Services;
using Skylab.Shared.Domain.Entities;

namespace Skylab.Shared.Infrastructure.Auth;

public class ExternalUserService : IExternalUserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExternalUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<UserContract?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<ExternalUser>>($"/internal/api/users/{userId}", _jsonOptions, cancellationToken);

            if (response != null && response.Success && response.Data != null)
            {
                return MapToContract(response.Data);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<UserContract>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (!distinctIds.Any()) return [];

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/internal/api/users/batch", distinctIds, _jsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ExternalUser>>>(_jsonOptions, cancellationToken);

            if (result != null && result.Success && result.Data != null)
                return result.Data.Select(MapToContract).ToList();

            return [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static UserContract MapToContract(ExternalUser user)
    {
        return new UserContract(
            user.Id,
            user.Email,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.ProfilePictureUrl
        );
    }
}
