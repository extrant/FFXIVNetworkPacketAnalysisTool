using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.PacketStructures
{
    // UP (发包) 结构体定义
    // 结构体名称必须与 OpcodeName 完全匹配（例如 UP_ClientTrigger, UP_ActionSend 等）

    /// <summary>
    /// 客户端触发器包
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct UP_ClientTrigger
    {
        [FieldOffset(0x0)] public uint id;
        [FieldOffset(0x4)] public uint arg0;
        [FieldOffset(0x8)] public uint arg1;
        [FieldOffset(0xC)] public uint arg2;
        [FieldOffset(0x10)] public uint arg3;
        [FieldOffset(0x18)] public ulong target_common_id;
    }

    /// <summary>
    /// 更新位置实例
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    public struct UP_UpdatePositionInstance
    {
        [FieldOffset(0x0)] public float facing;
        [FieldOffset(0x4)] public float predicted_facing;
        [FieldOffset(0x8)] public ushort flag;
        [FieldOffset(0xA)] public byte flag_2;
        [FieldOffset(0xC)] public float pos_x;
        [FieldOffset(0x10)] public float pos_y;
        [FieldOffset(0x14)] public float pos_z;
        [FieldOffset(0x18)] public float predicted_pos_x;
        [FieldOffset(0x1C)] public float predicted_pos_y;
        [FieldOffset(0x20)] public float predicted_pos_z;
    }

    /// <summary>
    /// 更新位置处理器
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x24)]
    public struct UP_UpdatePositionHandler
    {
        [FieldOffset(0x0)] public float facing;
        [FieldOffset(0x4)] public ushort flag;
        [FieldOffset(0x6)] public byte flag_2;
        [FieldOffset(0x8)] public float pos_x;
        [FieldOffset(0xC)] public float pos_y;
        [FieldOffset(0x10)] public float pos_z;
    }

    /// <summary>
    /// 事件开始
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct UP_EventStart
    {
        [FieldOffset(0x0)] public ulong target_common_id;
        [FieldOffset(0x8)] public uint handler_id;
    }

    /// <summary>
    /// 事件完成
    /// 注意：Python 中 _size_ = 0x10 表示基础结构体大小，args 是动态数组
    /// 实际包大小 = 0x10 + arg_cnt * 4
    /// 这里定义最大容量：0x10 (base) + 255 * 4 (args) = 0x408
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public unsafe struct UP_EventFinish
    {
        [FieldOffset(0x0)] public uint handler_id;
        [FieldOffset(0x4)] public ushort scene_id;
        [FieldOffset(0x6)] public byte error;
        [FieldOffset(0x7)] public byte arg_cnt;
        [FieldOffset(0x8)] public fixed uint args[255];  // 动态参数数组，实际使用前 arg_cnt 个
    }

    /// <summary>
    /// 事件动作
    /// 注意：Python 中 _size_ = 0x10 表示基础结构体大小，args 是动态数组
    /// 实际包大小 = 0x10 + arg_cnt * 4
    /// 这里定义最大容量：0x10 (base) + 255 * 4 (args) = 0x408
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public unsafe struct UP_EventAction
    {
        [FieldOffset(0x0)] public uint handler_id;
        [FieldOffset(0x4)] public ushort scene_id;
        [FieldOffset(0x6)] public byte res;
        [FieldOffset(0x7)] public byte arg_cnt;
        [FieldOffset(0x8)] public fixed uint args[255];  // 动态参数数组，实际使用前 arg_cnt 个
    }

    /// <summary>
    /// Ping 请求
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct UP_PingReq
    {
        [FieldOffset(0x0)] public uint time_ms;
    }

    /// <summary>
    /// 动作发送（目标）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct UP_ActionSend
    {
        [FieldOffset(0x0)] public byte cast_buff;
        [FieldOffset(0x1)] public byte action_kind;
        [FieldOffset(0x4)] public uint action_id;
        [FieldOffset(0x8)] public ushort request_id;
        [FieldOffset(0xA)] public ushort facing;
        [FieldOffset(0xC)] public ushort target_facing;
        [FieldOffset(0x10)] public ulong target_id;
        [FieldOffset(0x18)] public uint arg;
    }

    /// <summary>
    /// 动作发送（坐标）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct UP_ActionSendPos
    {
        [FieldOffset(0x0)] public byte cast_buff;
        [FieldOffset(0x1)] public byte action_kind;
        [FieldOffset(0x4)] public uint action_id;
        [FieldOffset(0x8)] public ushort request_id;
        [FieldOffset(0xA)] public ushort facing;
        [FieldOffset(0xC)] public ushort target_facing;
        [FieldOffset(0x10)] public float pos_x;
        [FieldOffset(0x14)] public float pos_y;
        [FieldOffset(0x18)] public float pos_z;
    }

    /// <summary>
    /// 物品栏修改处理器
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    public struct UP_InventoryModifyHandler
    {
        [FieldOffset(0x0)] public uint context_id;
        [FieldOffset(0x4)] public uint operation_type;
        [FieldOffset(0x8)] public uint src_entity;
        [FieldOffset(0xC)] public uint src_storage_id;
        [FieldOffset(0x10)] public short src_container_index;
        [FieldOffset(0x14)] public uint src_cnt;
        [FieldOffset(0x18)] public uint src_item_id;
        [FieldOffset(0x1C)] public uint dst_entity;
        [FieldOffset(0x20)] public uint dst_storage_id;
        [FieldOffset(0x24)] public short dst_container_index;
        [FieldOffset(0x28)] public uint dst_cnt;
        [FieldOffset(0x2C)] public uint dst_item_id;
    }
}
