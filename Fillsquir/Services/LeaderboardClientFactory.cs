using Microsoft.Maui.Storage;

namespace Fillsquir.Services;

internal static class LeaderboardClientFactory
{
    private const string DefaultBaseUrl = "http://localhost:5180/";
    private const string BaseUrlPreferenceKey = "LeaderboardApiBaseUrl";
    private const string BaseUrlEnvironmentVariable = "FSQUIR_API_BASE_URL";

    internal static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = ResolveBaseUri(),
            Timeout = TimeSpan.FromSeconds(2)
        };
    }

    private static Uri ResolveBaseUri()
    {
        string configured = Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable)
            ?? Preferences.Get(BaseUrlPreferenceKey, DefaultBaseUrl);

        configured = configured.Trim();
        if (!configured.EndsWith("/", StringComparison.Ordinal))
        {
            configured += "/";
        }

        return Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri)
            ? uri
            : new Uri(DefaultBaseUrl);
    }
}
