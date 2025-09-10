using System.Text;
using System.Text.Json;

namespace Chainly.Application.Services.Interfaces
{
    public interface IExternalApiService
    {
        Task<T?> GetAsync<T>(string url);
        Task<T?> PostAsync<T>(string url, object? data = null);
        Task<T?> GetByIdAsync<T>(string baseUrl, string id);
        Task<T?> PostWithIdAsync<T>(string baseUrl, string id, object? data = null);
    }
}
