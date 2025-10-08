using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.PacketStructures
{
    // ============================================
    // 包结构体定义指南
    // ============================================
    //
    // 现在包结构体已按方向分类到不同文件：
    //
    // 1. UpPacketStructs.cs   - 所有 UP (发包) 结构体，包头偏移 0x10 (16字节)
    // 2. DownPacketStructs.cs - 所有 DOWN (收包) 结构体，包头偏移 0x20 (32字节)
    //
    // 结构体命名规则：
    // - 发包结构体必须以 UP_ 开头（例如：UP_ClientTrigger）
    // - 收包结构体必须以 DOWN_ 开头（例如：DOWN_NpcSpawn）
    // - 结构体名称必须与 OpcodeName 完全匹配
    //
    // 注意事项：
    // - 使用 [StructLayout(LayoutKind.Explicit, Size = 0x...)] 特性
    // - 每个字段必须使用 [FieldOffset(0x...)] 指定偏移
    // - 偏移量是相对于结构体数据的开始位置（不包含包头）
    // - 如需使用固定长度数组，添加 unsafe 关键字并使用 fixed 修饰符
    //
    // ============================================

    /// <summary>
    /// 示例结构体（仅供参考，实际结构体请在对应文件中定义）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct EXAMPLE_PacketStruct
    {
        [FieldOffset(0x0)] public uint field1;
        [FieldOffset(0x4)] public ushort field2;
        [FieldOffset(0x8)] public ulong field3;
        [FieldOffset(0x10)] public float pos_x;
        [FieldOffset(0x14)] public float pos_y;
        [FieldOffset(0x18)] public float pos_z;
    }
}
