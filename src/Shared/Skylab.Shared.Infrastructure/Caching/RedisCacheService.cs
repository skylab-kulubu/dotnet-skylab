using StackExchange.Redis;
using System.Text.Json;

namespace Skylab.Shared.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry.HasValue ? expiry.Value : default);
    }
    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        await _db.KeyDeleteAsync(key);
    }
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Length > 0)
            await _db.KeyDeleteAsync(keys);
    }
    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        return await _db.KeyExistsAsync(key);
    }

}