using System.Text.Json;
using StackExchange.Redis;

namespace EFDotnet.Services;

public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = _redis.GetDatabase();
        var jsonData = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, jsonData, expiry);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var db = _redis.GetDatabase();
        var jsonData = await db.StringGetAsync(key);
        if (jsonData.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(jsonData);
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}