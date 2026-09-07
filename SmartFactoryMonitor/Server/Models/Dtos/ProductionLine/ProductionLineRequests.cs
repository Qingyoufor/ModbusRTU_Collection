using System.ComponentModel.DataAnnotations;

namespace Server.Models.Dtos
{
    public record CreateProductionLineRequest
    {
        [Required]
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    public record UpdateProductionLineRequest
    {
        [Required]
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
