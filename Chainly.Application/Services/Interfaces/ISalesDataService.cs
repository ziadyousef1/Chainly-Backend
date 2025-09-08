using Chainly.Application.DTOs;

namespace Chainly.Application.Services.Interfaces
{
    public interface ISalesDataService
    {
        Task<SalesDataResponseDto?> GetSalesDataByIdAsync(long id);
        Task<List<SalesDataResponseDto>> GetLast30DaysForProductAsync(long productId);
        Task<IEnumerable<SalesDataImportDto>> ReadSalesDataFromExcelAsync(Stream excelStream);
        Task<int> ImportSalesDataAsync(IEnumerable<SalesDataImportDto> salesData);
    }
}
