using Client.Infrastructure;
using Client.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Client.Service
{
    public class ProductionLineService : IProductionLineService
    {
        private readonly HttpClientBase _http;

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ProductionLineService(HttpClientBase http)
        {
            _http = http;  
        }
        public async Task<List<ProductionLineDto>?> GetAllAsync()
        {
            var response = await _http.GetAsync("api/productionlines");

            if(!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<ProductionLineDto>>>(_jsonOptions);

            return result?.Code == 200 ? result.Data : null;
        }
    }
}
