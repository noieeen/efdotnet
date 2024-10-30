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
        if (_redis == null || !_redis.IsConnected)
        {
            Console.WriteLine("Redis is not connected. Skipping cache set operation.");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var jsonData = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, jsonData, expiry);
        }
        catch (Exception ex)
        {
            // Log the error (you can use any logging framework here)
            Console.WriteLine($"Redis SetAsync error: {ex.Message}");
        }
    }

    public async Task<T> GetAsync<T>(string key)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            Console.WriteLine("Redis is not connected. Skipping cache set operation.");
            return default;
        }

        try
        {
            var db = _redis.GetDatabase();
            var jsonData = await db.StringGetAsync(key);
            if (!jsonData.IsNullOrEmpty)
            {
                return JsonSerializer.Deserialize<T>(jsonData);
            }
        }
        catch (Exception ex)
        {
            // Log the error (you can use any logging framework here)
            Console.WriteLine($"Redis GetAsync error: {ex.Message}");
        }

        return default;
    }

    public async Task RemoveAsync(string key)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            Console.WriteLine("Redis is not connected. Skipping cache set operation.");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            // Log the error (you can use any logging framework here)
            Console.WriteLine($"Redis RemoveAsync error: {ex.Message}");
        }
    }
}