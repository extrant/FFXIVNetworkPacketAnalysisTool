using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace FFXIVNetworkPacketAnalysisTool;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string GameVersion { get; set; } = "";

    public Dictionary<int, string> UpOpcodes { get; set; } = new();
    public Dictionary<int, string> DownOpcodes { get; set; } = new();

    // UI 设置 - 包分析工具
    public bool ShowSendPackets { get; set; } = true;
    public bool ShowReceivePackets { get; set; } = true;
    public bool ShowOnlyKnownOpcodes { get; set; } = false;
    public bool AutoScroll { get; set; } = true;
    public bool CaptureEnabled { get; set; } = true;

    // 性能设置
    public int MaxPacketsPerSession { get; set; } = 5000; // 单个会话最大包数量

    // 保存配置
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
