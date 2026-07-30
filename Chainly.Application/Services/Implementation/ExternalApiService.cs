using System.Text;
using System.Text.Json;
using Chainly.Application.Configurations;
using Chainly.Application.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly AiApiSettings _aiApiSettings;

        public ExternalApiService(IHttpClientFactory httpClientFactory, IOptions<AiApiSettings> aiApiSettings)
        {
            _httpClientFactory = httpClientFactory;
            _aiApiSettings = aiApiSettings.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private HttpClient CreateHttpClient(string url)
        {
            if (!string.IsNullOrWhiteSpace(_aiApiSettings.InsightsBaseUrl)
                && url.StartsWith(_aiApiSettings.InsightsBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                return new HttpClient(handler);
            }

            return _httpClientFactory.CreateClient();
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            using var httpClient = CreateHttpClient(url);
            var response = await httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            
            return default(T);
        }

        public async Task<T?> PostAsync<T>(string url, object? data = null)
        {
            using var httpClient = CreateHttpClient(url);
            StringContent? content = null;
            
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            
            return default(T);
        }

        public async Task<T?> GetByIdAsync<T>(string baseUrl, string id)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{id}";
            return await GetAsync<T>(url);
        }

        public async Task<T?> PostWithIdAsync<T>(string baseUrl, string id, object? data = null)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{id}";
            return await PostAsync<T>(url, data);
        }
    }
}