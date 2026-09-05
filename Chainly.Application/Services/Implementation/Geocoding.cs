using System.Collections.Concurrent;
using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;

namespace Chainly.Application.Services.Implementation;

internal static class Geocoding
{
    private const string Unavailable = "Location not available";
    private const string ApiKey = "0e99ae334bed4b85b7f75ff56a8d1226";

    private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> Cache = new();

    public static Task<string> ResolveAsync(IExternalApiService? api, decimal latitude, decimal longitude)
    {
        if (api is null || (latitude == 0 && longitude == 0))
            return Task.FromResult(Unavailable);

        var key = $"{latitude:F2},{longitude:F2}";
        return Cache.GetOrAdd(key, _ => new Lazy<Task<string>>(
            () => FetchAsync(api, latitude, longitude),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public static Task<string[]> ResolveManyAsync(
        IExternalApiService? api,
        IEnumerable<(decimal Latitude, decimal Longitude)> points)
    {
        return Task.WhenAll(points.Select(p => ResolveAsync(api, p.Latitude, p.Longitude)));
    }

    private static async Task<string> FetchAsync(IExternalApiService api, decimal latitude, decimal longitude)
    {
        var url =
            $"https://api.geoapify.com/v1/geocode/reverse?lat={latitude}&lon={longitude}&apiKey={ApiKey}";
        var geoResponse = await api.GetAsync<GeoapifyResponse>(url);
        return geoResponse?.Features?.FirstOrDefault()?.Properties?.Formatted ?? Unavailable;
    }
}
