using AutoMapper;
using Server.Infrastructure.Data;
using Server.Infrastructure.Repositories;
using Server.Models.Common;
using Server.Models.Dtos;
using Server.Models.Entities;

namespace Server.Services
{
    public class ProductionLineService : IProductionLineService
    {
        private readonly IProductionLineRepository _productionLineRepo;
        private readonly IMapper _mapper;

        public ProductionLineService(IProductionLineRepository productionLineRepo,IMapper mapper)
        {
            _productionLineRepo = productionLineRepo;
            _mapper = mapper;
        }
        public async Task<ApiResult<List<ProductionLineDto>>> GetAllAsync()
        {
            var entities = await _productionLineRepo.GetAllAsync();
            return ApiResult<List<ProductionLineDto>>.Success(_mapper.Map<List<ProductionLineDto>>(entities));
        }

        public async Task<ApiResult<ProductionLineDto?>> GetByIdAsync(int id)
        {
            var productionLine = await _productionLineRepo.GetByIdAsync(id);
            if(productionLine == null)
            {
                return ApiResult<ProductionLineDto?>.Fail(404, "产线不存在");
            }
            return ApiResult<ProductionLineDto?>.Success(_mapper.Map<ProductionLineDto>(productionLine));
        }

        public async Task<ApiResult<ProductionLineDto>> CreateAsync(CreateProductionLineRequest request)
        {
            // dto通过mapping -> entity,无需通过new手动创建productionLine
            var productionLine = _mapper.Map<ProductionLine>(request);
            productionLine.CreatedAt = DateTime.UtcNow;
            await _productionLineRepo.AddAsync(productionLine);
            await _productionLineRepo.SaveChangedAsync();
            return ApiResult<ProductionLineDto>.Success(_mapper.Map<ProductionLineDto>(productionLine),"产线创建成功");
        }

        public async Task<ApiResult<ProductionLineDto>> UpdateAsync(int id, UpdateProductionLineRequest request)
        {
            var productionLine = await _productionLineRepo.GetByIdAsync(id);
            if(productionLine == null)
            {
                return ApiResult<ProductionLineDto>.Fail(404, "产线不存在");
            }
            _mapper.Map(request, productionLine);
            // _productionLineRepo.Update(productionLine); 不需要Update,productionLine已经被EF Core追踪
            await _productionLineRepo.SaveChangedAsync();
            return ApiResult<ProductionLineDto>.Success(_mapper.Map<ProductionLineDto>(productionLine), "产线更新成功");
        }

        public async Task<ApiResult> DeleteAsync(int id)
        {
            var productionLine = await _productionLineRepo.GetByIdAsync(id);
            if (productionLine == null)
            {
                return ApiResult.Fail(404, "产线不存在");
            }

            // 如果产线下有设备，则允许删除
            if (productionLine.Devices.Any())
            {
                return ApiResult.Fail(400, "该产线下存在设备，无法删除");
            }

            await _productionLineRepo.DeleteAsync(id);
            await _productionLineRepo.SaveChangedAsync();
            return ApiResult.Success("产线删除成功");

        }

    }
}
