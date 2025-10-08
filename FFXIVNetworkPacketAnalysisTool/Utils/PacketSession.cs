using System;
using System.Collections.Generic;

namespace FFXIVNetworkPacketAnalysisTool.Utils
{
    /// <summary>
    /// 包捕获会话
    /// 每个会话都是独立的，可以像浏览器标签页一样管理
    /// </summary>
    public class PacketSession
    {
        public long SessionId { get; set; }
        public string SessionName { get; set; }
        public DateTime CreateTime { get; set; }
        public List<PacketInfo> Packets { get; set; }
        public bool IsActive { get; set; }

        public PacketSession(long sessionId, string name)
        {
            SessionId = sessionId;
            SessionName = name;
            CreateTime = DateTime.Now;
            Packets = new List<PacketInfo>();
            IsActive = true;
        }

        public string GetDisplayName()
        {
            return $"{SessionName} ({Packets.Count} 包)";
        }

        // 清理资源
        public void Dispose()
        {
            Packets.Clear();
            Packets = null;
        }
    }
}
