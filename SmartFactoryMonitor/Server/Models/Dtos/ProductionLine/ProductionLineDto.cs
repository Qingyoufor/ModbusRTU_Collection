namespace Server.Models.Dtos
{
    public class ProductionLineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int DeviceCount { get; set; }
    }
}
