using System.Text;
using System.Text.Json;
using Chainly.Application.Services.Interfaces;

namespace Chainly.Application.Services.Implementation
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExternalApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            using var httpClient = _httpClientFactory.CreateClient();
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
            using var httpClient = _httpClientFactory.CreateClient();
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