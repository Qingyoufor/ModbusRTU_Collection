using Client.Models.Dtos;

namespace Client.Service
{
    public interface ISystemService
    {
        Task<SystemInfoDto?> GetInfoAsync();
        Task<SystemLogsDto?> GetLogsAsync(int maxLines = 200);
        Task<List<SystemSettingsDto>?> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(List<SystemSettingsDto> settings);
        Task<bool> ReloadModbusAsync();
    }
}