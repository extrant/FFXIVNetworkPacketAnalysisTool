<style>
img {
  display: block;
  margin: 0 auto;
}
</style>

![LOGO](https://raw.githubusercontent.com/extrant/IMGSave/refs/heads/main/FFXivStorkLauncher/NPATool.png "LOGO")

# FFXIV Network Packet Analysis Tool

FFXIV Network Packet Analysis Tool 以下简称 FFXIV NPATool 是一个用于分析最终幻想14游戏内网络数据包的调试工具，主要面向开发人员使用。

## 贡献者

<a href="https://github.com/extrant/FFXIVNetworkPacketAnalysisTool/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=extrant/FFXIVNetworkPacketAnalysisTool" />
</a>

## 功能特性



- **实时网络包捕获**：捕获游戏客户端与服务器之间的网络数据包（收包/发包），并以具体时间轴显示。还能实现对于Opcode包名的解析，但这个功能依靠:\
 `https://github.com/extrant/FFXIV.EXE/blob/main/Opcode/all_opcodes.json`\
 实现，并不能保证Opcode的实时性。十分建议您自己实现一个Opcode解析方案，或者为FFXIV.EXE项目提供最新的Opcode解析。您可以加入 `https://discord.gg/g8QKPAnCBa` 并找到其中的 FFXIV NPATool 类别进行交流。

- **多会话管理**：支持创建多个独立的捕获会话，方便对比不同时段的数据。

- **数据包过滤**：
  - 按方向过滤（发包/收包）。
  - 按Opcode名称搜索。
  - 仅显示已知Opcode。

- **数据包详细分析**：
  - 十六进制数据查看。
  - 自动解析为结构体（需要预先定义对应的结构体）。
  - 显示字段偏移、类型和值。

- **便捷操作**：
  - 暂停/继续捕获。
  - 多选删除数据包。
  - 复制十六进制数据或C#字节数组格式。
  - 自动滚动到最新数据包。

## 安装

### 前置条件

- 已安装XIVLauncher和Dalamud
- C#开发环境（开发者需要）

### 使用库链接安装
- 将 `https://raw.githubusercontent.com/extrant/DalamudPlugins/main/pluginmaster.json`粘贴到第三方的库链接中，在插件安装器中寻找 FFXIV Network Packet Analysis Tool 然后进行安装。

### 从源码构建

1. 克隆本仓库
2. 使用Visual Studio 2022或JetBrains Rider打开解决方案
3. 编译项目（Debug或Release）
4. 生成的DLL位于 `bin/x64/[Debug|Release]/FFXIVNetworkPacketAnalysisTool.dll`

## 使用方法

### 基本操作

- 游戏内输入命令 `/FFNPAT` 打开主窗口
- 主窗口默认开始捕获网络数据包\
![基本主界面](https://raw.githubusercontent.com/extrant/IMGSave/refs/heads/main/FFXIV%20NPATool/%E5%9F%BA%E6%9C%AC%E4%B8%BB%E7%95%8C%E9%9D%A2.png "基本主界面")
### 界面说明

#### 会话管理标签栏
- 当前活跃会话会显示未保存标记（小圆点）
- 可以创建多个会话，每个会话独立记录数据包
- 非活跃会话可以关闭（至少保留一个会话）

#### 控制面板
- **暂停/继续**：控制是否实时捕获数据包
- **清空日志**：清空当前会话的所有数据包
- **新建会话**：创建新的捕获会话
- **删除选中**：删除选中的数据包（支持Ctrl多选、Shift范围选择）
- **自动滚动**：自动滚动到最新的数据包
- **启用捕获**：全局开关，控制是否进行网络包捕获

#### 数据包列表（左侧面板）
- 显示捕获到的所有数据包
- 蓝色背景：发包（客户端→服务器）
- 绿色背景：收包（服务器→客户端）
- 红色背景：已选中的数据包
- 支持多选操作（Ctrl点击、Shift范围选择）
- 右键单击可删除单个数据包
![收发包界面](https://raw.githubusercontent.com/extrant/IMGSave/refs/heads/main/FFXIV%20NPATool/%E6%94%B6%E5%8F%91%E5%8C%85%E7%95%8C%E9%9D%A2.png "收发包界面")

#### 数据包详情（右侧面板）
- 选中数据包后显示详细信息：
![数据包详情](https://raw.githubusercontent.com/extrant/IMGSave/refs/heads/main/FFXIV%20NPATool/%E9%80%89%E4%B8%AD%E5%8C%85%E4%BD%93%E8%A7%A3%E6%9E%90%E7%95%8C%E9%9D%A2.png "数据包详情")

**十六进制数据**标签页：
- 显示完整的十六进制转储
- 可复制为十六进制文本或C#字节数组格式

**结构体解析**标签页：
- 自动查找对应的结构体定义并解析字段
- 显示每个字段的偏移、类型和值
- 收包数据自动跳过0x20字节包头偏移
- 发包数据自动跳过0x20字节包头偏移

![结构体](https://raw.githubusercontent.com/extrant/IMGSave/refs/heads/main/FFXIV%20NPATool/%E7%BB%93%E6%9E%84%E4%BD%93%E7%95%8C%E9%9D%A2.png "结构体")

### 定义自定义结构体

要使用结构体解析功能，需要在代码中定义对应的结构体。结构体名称必须与Opcode名称一致。

示例：
```csharp
using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.PacketStructures;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public unsafe struct YourOpcodeName
{
    [FieldOffset(0x00)] public ushort Field1;
    [FieldOffset(0x02)] public ushort Field2;
    [FieldOffset(0x04)] public uint ObjectId;
    [FieldOffset(0x08)] public fixed uint Args[4];
    // ...更多字段
}
```

### 配置选项

配置文件位于Dalamud配置目录，包含以下设置：

- `ShowSendPackets`：显示发包
- `ShowReceivePackets`：显示收包
- `ShowOnlyKnownOpcodes`：仅显示已知Opcode
- `AutoScroll`：自动滚动
- `CaptureEnabled`：启用捕获
- `MaxPacketsPerSession`：单个会话最大包数量（默认5000）


