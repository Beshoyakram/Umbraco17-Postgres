using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Centrocdx.Services;

public interface IFormSubmissionRateLimiter
{
    bool IsAllowed(string bucket, string clientKey, int maxAttempts, TimeSpan window, out string? errorMessage);

    void Record(string bucket, string clientKey, TimeSpan window);
}

/// <summary>
/// In-memory sliding window limiter for public form endpoints.
/// </summary>
public class FormSubmissionRateLimiter : IFormSubmissionRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.Ordinal);

    public FormSubmissionRateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsAllowed(
        string bucket,
        string clientKey,
        int maxAttempts,
        TimeSpan window,
        out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(clientKey) || maxAttempts <= 0 || window <= TimeSpan.Zero)
        {
            return true;
        }

        var cacheKey = BuildKey(bucket, clientKey);
        var gate = _locks.GetOrAdd(cacheKey, _ => new object());

        lock (gate)
        {
            var attempts = GetRecentAttempts(cacheKey, window);
            if (attempts.Count >= maxAttempts)
            {
                errorMessage = "Too many submissions. Please try again in about an hour.";
                return false;
            }

            return true;
        }
    }

    public void Record(string bucket, string clientKey, TimeSpan window)
    {
        if (string.IsNullOrWhiteSpace(clientKey) || window <= TimeSpan.Zero)
        {
            return;
        }

        var cacheKey = BuildKey(bucket, clientKey);
        var gate = _locks.GetOrAdd(cacheKey, _ => new object());

        lock (gate)
        {
            var attempts = GetRecentAttempts(cacheKey, window);
            attempts.Add(DateTimeOffset.UtcNow);
            _cache.Set(cacheKey, attempts, window);
        }
    }

    private List<DateTimeOffset> GetRecentAttempts(string cacheKey, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window;
        var attempts = _cache.Get<List<DateTimeOffset>>(cacheKey) ?? [];
        attempts.RemoveAll(x => x < cutoff);
        return attempts;
    }

    private static string BuildKey(string bucket, string clientKey)
        => $"form-rate:{bucket}:{clientKey.Trim().ToLowerInvariant()}";
}
