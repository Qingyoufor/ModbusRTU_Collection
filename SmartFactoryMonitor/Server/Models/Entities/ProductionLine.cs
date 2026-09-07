
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models.Entities
{
    /// <summary>产线（设备按产线分组管理）</summary>
    [Table("ProductionLines")]
    public class ProductionLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                    // 主键

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;// 产线名称

        [MaxLength(50)]
        public string? Description { get; set; }       // 产线描述

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 创建时间

        public ICollection<Device> Devices { get; set; } = new List<Device>(); // 该产线下的设备
    }
}
