using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Text;

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
        [FieldOffset(0x0)] public ClientTriggerFlag Flag;
        [FieldOffset(0x4)] public uint arg0;
        [FieldOffset(0x8)] public uint arg1;
        [FieldOffset(0xC)] public uint arg2;
        [FieldOffset(0x10)] public uint arg3;
    }

    /// <summary>
    /// 更新位置实例
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    public struct UP_UpdatePositionInstance
    {
        [FieldOffset(0x0)] public float Rotation;

        [FieldOffset(0x4)] public float RotationNew;

        [FieldOffset(0x8)] public MoveType flag;

        [FieldOffset(0xC)] public Vector3 Position;

        [FieldOffset(0x18)] public Vector3 PositionNew;

        public enum MoveType : uint
        {
            NormalMove0 = 0,
            NormalMove1 = 0x10000,
            NormalMove2 = 0x20000,
            NormalMove3 = 0x30000,
            ActionMove0 = 0x200000,
            Fly0 = 1,
            Fly1 = 0x10001,
            Fly2 = 0x20001,
            Fly3 = 0x30001,
            WalkOrSlowSwim0 = 2,
            WalkOrSlowSwim1 = 0x10002,
            WalkOrSlowSwim2 = 0x20002,
            WalkOrSlowSwim3 = 0x30002,
            SlowFly0 = 3,
            SlowFly1 = 0x10003,
            SlowFly2 = 0x20003,
            SlowFly3 = 0x30003,
            ActionMoveEnd0 = 0x1000,
            SmallMove0 = 0x404000,
            SmallMove1 = 0x414000,
            SmallMove2 = 0x424000,
            SmallMove3 = 0x434000,
            SmallFlight0 = 0x404001,
            SmallFlight1 = 0x414001,
            SmallFlight2 = 0x424001,
            SmallFlight3 = 0x434001,
            Falling0 = 0x100000,
            Falling1 = 0x110000,
            Falling2 = 0x120000,
            Falling3 = 0x130000,
            JumpStart0 = 0x400100,
            JumpStart1 = 0x410100,
            JumpStart2 = 0x420100,
            JumpStart3 = 0x430100,
            JumpProcess0 = 0x504000,
            JumpProcess1 = 0x514000,
            JumpProcess2 = 0x524000,
            JumpProcess3 = 0x534000,
            JumpHighestPoint0 = 0x510400,
            JumpEnd0 = 0x400200,
            JumpEnd1 = 0x410200,
            JumpEnd2 = 0x420200,
            JumpEnd3 = 0x430200,
        }
    }

    /// <summary>
    /// 更新位置处理器
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x28)]
    public struct UP_UpdatePositionHandler
    {
        [FieldOffset(0x0)] public float Rotation;
        [FieldOffset(0x4)] public MoveType Flag;

        [FieldOffset(0x8)] public Vector3 Position;
        
        public enum MoveType : uint
        {
            NormalMove0 = 0,
            NormalMove1 = 0x10000,
            NormalMove2 = 0x20000,
            NormalMove3 = 0x30000,
            Fly0 = 1,
            Fly1 = 0x10001,
            Fly2 = 0x20001,
            Fly3 = 0x30001,
            WalkOrSlowSwim0 = 2,
            WalkOrSlowSwim1 = 0x10002,
            WalkOrSlowSwim2 = 0x20002,
            WalkOrSlowSwim3 = 0x30002,
            SlowFly0 = 3,
            SlowFly1 = 0x10003,
            SlowFly2 = 0x20003,
            SlowFly3 = 0x30003,
            JumpStart0 = 0x100,
            JumpStart1 = 0x10100,
            JumpStart2 = 0x20100,
            JumpStart3 = 0x30100,
            JumpStartWalk0 = 0x102,
            JumpStartWalk1 = 0x10102,
            JumpStartWalk2 = 0x20102,
            JumpStartWalk3 = 0x30102,
            JumpEnd0 = 0x200,
            JumpEnd1 = 0x10200,
            JumpEnd2 = 0x20200,
            JumpEnd3 = 0x30200,
            JumpEndWalk0 = 0x202,
            JumpEndWalk1 = 0x10202,
            JumpEndWalk2 = 0x20202,
            JumpEndWalk3 = 0x30202,
            JumpProcess0 = 0x100000,
            JumpProcess1 = 0x110000,
            JumpProcess2 = 0x120000,
            JumpProcess3 = 0x130000,
            JumpProcessWalk0 = 0x100002,
            JumpProcessWalk1 = 0x110002,
            JumpProcessWalk2 = 0x120002,
            JumpProcessWalk3 = 0x130002,
            JumpHighestPoint0 = 0x100400,
            JumpHighestPoint1 = 0x110400,
            JumpHighestPoint2 = 0x120400,
            JumpHighestPoint3 = 0x130400,
            JumpHighestPointWalk0 = 0x100402,
            JumpHighestPointWalk1 = 0x110402,
            JumpHighestPointWalk2 = 0x120402,
            JumpHighestPointWalk3 = 0x130402,
            ActionMove0 = 0x200000,
            ActionMove1 = 0x210000,
            ActionMove2 = 0x220000,
            ActionMove3 = 0x230000,
            ActionMoveEnd0 = 0x1000,
            ActionMoveEnd1 = 0x11000,
            ActionMoveEnd2 = 0x21000,
            ActionMoveEnd3 = 0x31000
        }
    }

    /// <summary>
    /// 事件开始
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct UP_EventStart
    {
        [FieldOffset(0x0)] public ulong GameObjectID;

        [FieldOffset(0x8)] public uint EventID;
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

    /// <summary>
    /// 潜水TP包
    /// </summary>
    [StructLayout((LayoutKind.Explicit), Size = 0x30)]
    public unsafe struct UP_DiveStart
    {
        [FieldOffset(0x0)] public float Rotation;
        [FieldOffset(0x4)] public Vector3 Position;
    }
    
    /// <summary>
    /// 公共频道发言 
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1072)]
    public unsafe struct UP_ChatHandler
    {
        [FieldOffset(0)]
        public int a1; // 默认是0

        [FieldOffset(4)]
        public uint EntityID;   // 自己的Eid

        [FieldOffset(8)]
        public Vector3 position; // 坐标

        [FieldOffset(20)]
        public float rotation; // 方向

        [FieldOffset(24)]
        public XivChatType xivChatType; // 频道

        [FieldOffset(26)]
        public fixed byte Utf8string[1046];

    }
}
