using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Models
{
    public class ProductionLineTreeNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<DeviceTreeNode> Devices { get; set; } = new();
    }
}
