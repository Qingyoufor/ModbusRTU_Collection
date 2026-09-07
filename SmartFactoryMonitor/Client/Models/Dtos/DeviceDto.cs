using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Models.Dtos
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int ProductionLineId { get; set; }
        public string? ProductionLineName { get; set; }
        public byte SlaveAddress { get; set; }
        public string ComPort { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
