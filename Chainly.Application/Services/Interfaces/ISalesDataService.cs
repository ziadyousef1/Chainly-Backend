using Chainly.Application.DTOs;

namespace Chainly.Application.Services.Interfaces
{
    public interface ISalesDataService
    {
        Task<SalesDataResponseDto?> GetSalesDataByIdAsync(long id);
        Task<List<SalesDataResponseDto>> GetFilteredSalesDataAsync(SalesDataFilterDto filter);
        Task<IEnumerable<SalesDataImportDto>> ReadSalesDataFromExcelAsync(Stream excelStream);
        Task<int> ImportSalesDataAsync(IEnumerable<SalesDataImportDto> salesData);
    }
}
