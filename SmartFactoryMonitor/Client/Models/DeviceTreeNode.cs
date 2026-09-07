using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Models
{
    public class DeviceTreeNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";

        /// <summary>状态图标颜色：Running=绿色 / Alarm=红色 / 其他=灰色</summary>
        public string StatusColor => Status switch
        {
            "Running" => "#4CAF50",
            "Alarm" => "#F44336",
            _ => "#9E9E9E"
        };
    }
}
