using Server.Models.Common;
using Server.Models.Dtos;

namespace Server.Services
{
    public interface IProductionLineService
    {
        Task<ApiResult<List<ProductionLineDto>>> GetAllAsync();
        Task<ApiResult<ProductionLineDto?>> GetByIdAsync(int id);
        Task<ApiResult<ProductionLineDto>> CreateAsync(CreateProductionLineRequest request);
        Task<ApiResult<ProductionLineDto>> UpdateAsync(int id, UpdateProductionLineRequest request);
        Task<ApiResult> DeleteAsync(int id);
    }
}
