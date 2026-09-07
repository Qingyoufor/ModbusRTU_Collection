using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.Dtos;
using Server.Services;

namespace Server.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionLinesController : ControllerBase
    {
        private readonly IProductionLineService _service;

        public ProductionLinesController(IProductionLineService service) => _service = service;

        /// <summary>GET api/production-lines — 获取所有产线</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>GET api/production-lines/{id} — 获取产线详情</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Code == 200 ? Ok(result) : NotFound(result);
        }

        /// <summary>POST api/production-lines — 创建产线</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateProductionLineRequest request)
        {
            var result = await _service.CreateAsync(request);
            return result.Code == 200 ? Ok(result) : BadRequest(result);
        }

        /// <summary>PUT api/production-lines/{id} — 更新产线</summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductionLineRequest request)
        {
            var result = await _service.UpdateAsync(id, request);
            return result.Code == 200 ? Ok(result) : (result.Code == 404 ? NotFound(result) : BadRequest(result));
        }

        /// <summary>DELETE api/production-lines/{id} — 删除产线</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Code == 200 ? Ok(result) : (result.Code == 404 ? NotFound(result) : BadRequest(result));
        }
    }
}
