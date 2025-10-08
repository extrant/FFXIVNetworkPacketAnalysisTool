using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.PacketStructures
{
    // DOWN (收包) 结构体定义
    // 结构体名称必须与 OpcodeName 完全匹配（例如 DOWN_NpcSpawn, DOWN_ActorControl 等）

    /// <summary>
    /// NPC 生成包结构体
    /// 此结构体定义了 DOWN_NpcSpawn 包的数据布局
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x290)]
    public unsafe struct DOWN_NpcSpawn
    {
        [FieldOffset(0x10)] public ulong MainTarget;
        [FieldOffset(0x40)] public uint DataID;
        [FieldOffset(0x44)] public uint OwnerID;
        [FieldOffset(0x54)] public uint FlagOrState;
        [FieldOffset(0x5C)] public uint CurrentHp;
        [FieldOffset(0x60)] public uint MaxHp;
        [FieldOffset(0x72)] public float Rotation;
        [FieldOffset(0x7F)] public byte ModelScale; //不确定
        [FieldOffset(0x81)] public byte ObjectKind;
        [FieldOffset(0x82)] public byte ObjectType;
        [FieldOffset(0x85)] public byte BattalionType;
        [FieldOffset(0x86)] public byte Level;
        [FieldOffset(0x200)] public float PosX;
        [FieldOffset(0x204)] public float PosY;
        [FieldOffset(0x208)] public float PosZ;



        [FieldOffset(0x242)] public fixed byte NameBytes[32];
    }

    /// <summary>
    /// 技能效果嵌套结构体（单个效果）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public struct ActionEffectEntry
    {
        [FieldOffset(0x00)] public byte Type;
        [FieldOffset(0x01)] public byte Arg0;
        [FieldOffset(0x02)] public byte Arg1;
        [FieldOffset(0x03)] public byte Arg2;
        [FieldOffset(0x04)] public byte Arg3;
        [FieldOffset(0x05)] public byte Flag;
        [FieldOffset(0x06)] public ushort Value;
    }

    /// <summary>
    /// 单目标技能效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x78)]
    public unsafe struct DOWN_Effect
    {
        [FieldOffset(0x00)] public uint MainTargetId;
        [FieldOffset(0x08)] public uint RealActionId;
        [FieldOffset(0x0C)] public uint ResponseId;
        [FieldOffset(0x10)] public float LockTime;
        [FieldOffset(0x14)] public uint BallistaTargetId;
        [FieldOffset(0x18)] public ushort RequestId;
        [FieldOffset(0x1A)] public ushort Facing;
        [FieldOffset(0x1C)] public ushort ActionId;
        [FieldOffset(0x1E)] public byte ActionVariant;
        [FieldOffset(0x1F)] public byte ActionKind;
        [FieldOffset(0x20)] public byte Flag;
        [FieldOffset(0x21)] public byte TargetCount;
        [FieldOffset(0x2A)] public fixed byte Effects[64]; // 8 个 ActionEffectEntry (8 * 8 = 64 bytes)
        [FieldOffset(0x70)] public ulong TargetId;

        /// <summary>
        /// 包修正：调整 RealActionId（加上基准值）
        /// Python: self.real_action_id += v
        /// </summary>
        public void ApplyPacketFix(uint baseValue)
        {
            RealActionId += baseValue;
        }
    }

    /// <summary>
    /// 8目标AOE技能效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x278)]
    public unsafe struct DOWN_AoeEffect8
    {
        [FieldOffset(0x00)] public uint MainTargetId;
        [FieldOffset(0x08)] public uint RealActionId;
        [FieldOffset(0x0C)] public uint ResponseId;
        [FieldOffset(0x10)] public float LockTime;
        [FieldOffset(0x14)] public uint BallistaTargetId;
        [FieldOffset(0x18)] public ushort RequestId;
        [FieldOffset(0x1A)] public ushort Facing;
        [FieldOffset(0x1C)] public ushort ActionId;
        [FieldOffset(0x1E)] public byte ActionVariant;
        [FieldOffset(0x1F)] public byte ActionKind;
        [FieldOffset(0x20)] public byte Flag;
        [FieldOffset(0x21)] public byte TargetCount;
        [FieldOffset(0x2A)] public fixed byte Effects[512]; // 8 目标 * 8 效果 * 8 字节
        [FieldOffset(0x230)] public fixed ulong TargetIds[8];
        [FieldOffset(0x270)] public fixed ushort Pos[3];

        /// <summary>
        /// 包修正：调整 RealActionId
        /// </summary>
        public void ApplyPacketFix(uint baseValue) => RealActionId += baseValue;
    }

    /// <summary>
    /// 16目标AOE技能效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x4B8)]
    public unsafe struct DOWN_AoeEffect16
    {
        [FieldOffset(0x00)] public uint MainTargetId;
        [FieldOffset(0x08)] public uint RealActionId;
        [FieldOffset(0x0C)] public uint ResponseId;
        [FieldOffset(0x10)] public float LockTime;
        [FieldOffset(0x14)] public uint BallistaTargetId;
        [FieldOffset(0x18)] public ushort RequestId;
        [FieldOffset(0x1A)] public ushort Facing;
        [FieldOffset(0x1C)] public ushort ActionId;
        [FieldOffset(0x1E)] public byte ActionVariant;
        [FieldOffset(0x1F)] public byte ActionKind;
        [FieldOffset(0x20)] public byte Flag;
        [FieldOffset(0x21)] public byte TargetCount;
        [FieldOffset(0x2A)] public fixed byte Effects[1024]; // 16 目标 * 8 效果 * 8 字节
        [FieldOffset(0x430)] public fixed ulong TargetIds[16];
        [FieldOffset(0x4B0)] public fixed ushort Pos[3];

        public void ApplyPacketFix(uint baseValue) => RealActionId += baseValue;
    }

    /// <summary>
    /// 24目标AOE技能效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x6F8)]
    public unsafe struct DOWN_AoeEffect24
    {
        [FieldOffset(0x00)] public uint MainTargetId;
        [FieldOffset(0x08)] public uint RealActionId;
        [FieldOffset(0x0C)] public uint ResponseId;
        [FieldOffset(0x10)] public float LockTime;
        [FieldOffset(0x14)] public uint BallistaTargetId;
        [FieldOffset(0x18)] public ushort RequestId;
        [FieldOffset(0x1A)] public ushort Facing;
        [FieldOffset(0x1C)] public ushort ActionId;
        [FieldOffset(0x1E)] public byte ActionVariant;
        [FieldOffset(0x1F)] public byte ActionKind;
        [FieldOffset(0x20)] public byte Flag;
        [FieldOffset(0x21)] public byte TargetCount;
        [FieldOffset(0x2A)] public fixed byte Effects[1536]; // 24 目标 * 8 效果 * 8 字节
        [FieldOffset(0x630)] public fixed ulong TargetIds[24];
        [FieldOffset(0x6F0)] public fixed ushort Pos[3];

        public void ApplyPacketFix(uint baseValue) => RealActionId += baseValue;
    }

    /// <summary>
    /// 32目标AOE技能效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x938)]
    public unsafe struct DOWN_AoeEffect32
    {
        [FieldOffset(0x00)] public uint MainTargetId;
        [FieldOffset(0x08)] public uint RealActionId;
        [FieldOffset(0x0C)] public uint ResponseId;
        [FieldOffset(0x10)] public float LockTime;
        [FieldOffset(0x14)] public uint BallistaTargetId;
        [FieldOffset(0x18)] public ushort RequestId;
        [FieldOffset(0x1A)] public ushort Facing;
        [FieldOffset(0x1C)] public ushort ActionId;
        [FieldOffset(0x1E)] public byte ActionVariant;
        [FieldOffset(0x1F)] public byte ActionKind;
        [FieldOffset(0x20)] public byte Flag;
        [FieldOffset(0x21)] public byte TargetCount;
        [FieldOffset(0x2A)] public fixed byte Effects[2048]; // 32 目标 * 8 效果 * 8 字节
        [FieldOffset(0x830)] public fixed ulong TargetIds[32];
        [FieldOffset(0x930)] public fixed ushort Pos[3];

        public void ApplyPacketFix(uint baseValue) => RealActionId += baseValue;
    }

    /// <summary>
    /// Actor施法包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public unsafe struct DOWN_ActorCast
    {
        [FieldOffset(0x00)] public ushort ActionId;
        [FieldOffset(0x02)] public byte ActionKind;
        [FieldOffset(0x03)] public byte DisplayDelay;
        [FieldOffset(0x04)] public uint RealActionId;
        [FieldOffset(0x08)] public float CastTime;
        [FieldOffset(0x0C)] public uint TargetId;
        [FieldOffset(0x10)] public ushort Facing;
        [FieldOffset(0x12)] public byte CanInterrupt;
        [FieldOffset(0x18)] public fixed ushort Pos[3];

        /// <summary>
        /// 包修正：调整 RealActionId
        /// Python: self.real_action_id += v
        /// </summary>
        public void ApplyPacketFix(uint baseValue) => RealActionId += baseValue;
    }

    /// <summary>
    /// Actor控制包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct DOWN_ActorControl
    {
        [FieldOffset(0x00)] public ushort Id; // ActorControlId 枚举值
        [FieldOffset(0x04)] public uint Arg0;
        [FieldOffset(0x08)] public uint Arg1;
        [FieldOffset(0x0C)] public uint Arg2;
        [FieldOffset(0x10)] public uint Arg3;

        /// <summary>
        /// 获取控制类型枚举
        /// </summary>
        public ActorControlId ControlId => (ActorControlId)Id;

        /// <summary>
        /// 获取控制类型名称（用于UI显示）
        /// </summary>
        public string ControlIdName
        {
            get
            {
                if (System.Enum.IsDefined(typeof(ActorControlId), Id))
                    return ((ActorControlId)Id).ToString();
                return $"Unknown(0x{Id:X4})";
            }
        }

        /// <summary>
        /// 包修正：根据控制类型调整参数值
        /// 用于修正相对ID为绝对ID（例如 SetLockOn 时 Arg0 需要加上基准值）
        /// </summary>
        public void ApplyPacketFix(uint baseValue)
        {
            if (ControlId == ActorControlId.SetLockOn)
            {
                Arg0 += baseValue;
            }
        }
    }

    /// <summary>
    /// Actor自身控制包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct DOWN_ActorControlSelf
    {
        [FieldOffset(0x00)] public ushort Id; // ActorControlId 枚举值
        [FieldOffset(0x04)] public uint Arg0;
        [FieldOffset(0x08)] public uint Arg1;
        [FieldOffset(0x0C)] public uint Arg2;
        [FieldOffset(0x10)] public uint Arg3;
        [FieldOffset(0x14)] public uint Arg4;
        [FieldOffset(0x18)] public uint Arg5;

        /// <summary>
        /// 获取控制类型枚举
        /// </summary>
        public ActorControlId ControlId => (ActorControlId)Id;

        /// <summary>
        /// 获取控制类型名称（用于UI显示）
        /// </summary>
        public string ControlIdName
        {
            get
            {
                if (System.Enum.IsDefined(typeof(ActorControlId), Id))
                    return ((ActorControlId)Id).ToString();
                return $"Unknown(0x{Id:X4})";
            }
        }
    }

    /// <summary>
    /// Actor目标控制包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public struct DOWN_ActorControlTarget
    {
        [FieldOffset(0x00)] public ushort Id; // ActorControlId 枚举值
        [FieldOffset(0x04)] public uint Arg0;
        [FieldOffset(0x08)] public uint Arg1;
        [FieldOffset(0x0C)] public uint Arg2;
        [FieldOffset(0x10)] public uint Arg3;
        [FieldOffset(0x18)] public ulong TargetId;

        /// <summary>
        /// 获取控制类型枚举
        /// </summary>
        public ActorControlId ControlId => (ActorControlId)Id;

        /// <summary>
        /// 获取控制类型名称（用于UI显示）
        /// </summary>
        public string ControlIdName
        {
            get
            {
                if (System.Enum.IsDefined(typeof(ActorControlId), Id))
                    return ((ActorControlId)Id).ToString();
                return $"Unknown(0x{Id:X4})";
            }
        }
    }

    /// <summary>
    /// Actor删除包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public struct DOWN_ActorDelete
    {
        [FieldOffset(0x00)] public byte Index;
        [FieldOffset(0x04)] public uint ActorId;
    }

    /// <summary>
    /// Actor量表包结构体（职业特殊量表）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public unsafe struct DOWN_ActorGauge
    {
        [FieldOffset(0x00)] public fixed byte Buffer[16];
    }

    /// <summary>
    /// 更新HP/MP/GP包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x08)]
    public struct DOWN_UpdateHpMpGp
    {
        [FieldOffset(0x00)] public uint Hp;
        [FieldOffset(0x04)] public ushort Mp;
        [FieldOffset(0x06)] public ushort Gp;
    }

    /// <summary>
    /// 效果结果状态结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct EffectResultStatus
    {
        [FieldOffset(0x00)] public byte StatusSlot;
        [FieldOffset(0x02)] public ushort StatusId;
        [FieldOffset(0x04)] public short Param;
        [FieldOffset(0x08)] public float Time;
        [FieldOffset(0x0C)] public uint SourceId;
    }

    /// <summary>
    /// 单个效果结果条目
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x58)]
    public unsafe struct EffectResultEntry
    {
        [FieldOffset(0x00)] public uint ResponseId;
        [FieldOffset(0x04)] public uint TargetId;
        [FieldOffset(0x08)] public uint CurrentHp;
        [FieldOffset(0x0C)] public uint MaxHp;
        [FieldOffset(0x10)] public ushort CurrentMp;
        [FieldOffset(0x13)] public byte ClassJob;
        [FieldOffset(0x14)] public byte Shield;
        [FieldOffset(0x15)] public byte StatusCount;

        // 状态数组 - 使用结构化数组而非 fixed byte
        [FieldOffset(0x18)] public EffectResultStatus Status0;
        [FieldOffset(0x28)] public EffectResultStatus Status1;
        [FieldOffset(0x38)] public EffectResultStatus Status2;
        [FieldOffset(0x48)] public EffectResultStatus Status3;

        /// <summary>
        /// 获取指定索引的状态（根据 StatusCount 使用）
        /// </summary>
        public EffectResultStatus GetStatus(int index)
        {
            return index switch
            {
                0 => Status0,
                1 => Status1,
                2 => Status2,
                3 => Status3,
                _ => throw new System.IndexOutOfRangeException("Status index must be 0-3")
            };
        }
    }

    /// <summary>
    /// 效果结果包结构体
    /// Python 中定义为可变长度，最多支持 16 个结果条目
    /// 实际大小: 0x04 (header) + 16 * 0x58 (results) = 0x584
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x584)]
    public unsafe struct DOWN_EffectResult
    {
        [FieldOffset(0x00)] public byte Count;
        [FieldOffset(0x04)] public fixed byte Results[1408]; // 16 个 EffectResultEntry (16 * 0x58 = 0x580)

        /// <summary>
        /// 获取指定索引的 EffectResultEntry（需要手动解析 fixed byte 数组）
        /// </summary>
        public EffectResultEntry GetResult(int index)
        {
            if (index < 0 || index >= Count)
                throw new System.IndexOutOfRangeException();

            fixed (byte* ptr = Results)
            {
                return ((EffectResultEntry*)(ptr + index * 0x58))[0];
            }
        }
    }

    /// <summary>
    /// 地图效果包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct DOWN_MapEffect
    {
        [FieldOffset(0x00)] public uint DirectorId;
        [FieldOffset(0x04)] public ushort State;
        [FieldOffset(0x06)] public ushort PlayState;
        [FieldOffset(0x08)] public byte Index;
    }

    /// <summary>
    /// 状态（Buff/Debuff）结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x0C)]
    public struct Status
    {
        [FieldOffset(0x00)] public ushort BuffId;
        [FieldOffset(0x02)] public short Param;
        [FieldOffset(0x04)] public float Timer;
        [FieldOffset(0x08)] public uint ActorId;
    }

    /// <summary>
    /// 玩家生成包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x278)]
    public unsafe struct DOWN_PlayerSpawn
    {
        [FieldOffset(0x00)] public ushort TitleId;
        [FieldOffset(0x02)] public ushort PlayingActionTimelineId;
        [FieldOffset(0x04)] public ushort WorldId;
        [FieldOffset(0x06)] public ushort HomeWorldId;
        [FieldOffset(0x08)] public byte GmLevel;
        [FieldOffset(0x09)] public byte GrandCompany;
        [FieldOffset(0x0A)] public byte GrandCompanyLevel;
        [FieldOffset(0x0B)] public byte OnlineStatus;
        [FieldOffset(0x0C)] public byte PoseEmote;
    }

    /// <summary>
    /// NPC生成2包结构体（扩展状态）
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x3F0)]
    public unsafe struct DOWN_NpcSpawn2
    {
        // 继承 NpcSpawn 的所有字段（0x290）
        [FieldOffset(0x10)] public ulong MainTarget;
        [FieldOffset(0x40)] public uint DataID;
        [FieldOffset(0x44)] public uint OwnerID;
        [FieldOffset(0x54)] public uint FlagOrState;
        [FieldOffset(0x5C)] public uint CurrentHp;
        [FieldOffset(0x60)] public uint MaxHp;
        [FieldOffset(0x72)] public float Rotation;
        [FieldOffset(0x7F)] public byte ModelScale;
        [FieldOffset(0x81)] public byte ObjectKind;
        [FieldOffset(0x82)] public byte ObjectType;
        [FieldOffset(0x85)] public byte BattalionType;
        [FieldOffset(0x86)] public byte Level;
        [FieldOffset(0x200)] public float PosX;
        [FieldOffset(0x204)] public float PosY;
        [FieldOffset(0x208)] public float PosZ;
        [FieldOffset(0x242)] public fixed byte NameBytes[32];

        // 扩展状态数组（30 个 Status，从 0x284 开始）
        [FieldOffset(0x284)] public fixed byte ExpandStatus[360]; // 30 * 0x0C
    }

    /// <summary>
    /// 对象生成包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    public unsafe struct DOWN_ObjectSpawn
    {
        [FieldOffset(0x00)] public byte Index;
        [FieldOffset(0x01)] public byte Kind;
        [FieldOffset(0x02)] public byte Flag;
        [FieldOffset(0x03)] public byte InvisibilityGroup;
        [FieldOffset(0x04)] public uint BaseId;
        [FieldOffset(0x08)] public uint Id;
        [FieldOffset(0x0C)] public uint LayoutId;
        [FieldOffset(0x10)] public uint ContentId;
        [FieldOffset(0x14)] public uint OwnerId;
        [FieldOffset(0x18)] public uint BindLayoutId;
        [FieldOffset(0x1C)] public float Scale;
        [FieldOffset(0x20)] public ushort SharedGroupTimelineState;
        [FieldOffset(0x22)] public ushort Facing;
        [FieldOffset(0x24)] public ushort Fate;
        [FieldOffset(0x26)] public byte PermissionInvisibility;
        [FieldOffset(0x27)] public byte Arg1;
        [FieldOffset(0x28)] public uint Arg2;
        [FieldOffset(0x2C)] public uint Arg3;
        [FieldOffset(0x30)] public fixed float Pos[3];
    }

    /// <summary>
    /// Actor移动包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x0C)]
    public unsafe struct DOWN_ActorMove
    {
        [FieldOffset(0x00)] public ushort Facing;
        [FieldOffset(0x02)] public ushort Flag;
        [FieldOffset(0x04)] public byte Speed;
        [FieldOffset(0x06)] public fixed ushort Pos[3];
    }

    /// <summary>
    /// Actor设置位置包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public unsafe struct DOWN_ActorSetPos
    {
        [FieldOffset(0x00)] public ushort Facing;
        [FieldOffset(0x02)] public byte Type;
        [FieldOffset(0x03)] public byte TypeArg;
        [FieldOffset(0x04)] public uint LayerId;
        [FieldOffset(0x08)] public fixed float Pos[3];
    }

    /// <summary>
    /// 事件开始包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct DOWN_EventStart
    {
        [FieldOffset(0x00)] public ulong TargetCommonId;
        [FieldOffset(0x08)] public uint HandlerId;
        [FieldOffset(0x0C)] public byte Type;
        [FieldOffset(0x0D)] public byte Flags;
        [FieldOffset(0x10)] public uint Arg;
    }

    /// <summary>
    /// 事件结束包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct DOWN_EventFinish
    {
        [FieldOffset(0x00)] public uint HandlerId;
        [FieldOffset(0x04)] public byte Type;
        [FieldOffset(0x05)] public byte Res;
        [FieldOffset(0x08)] public uint Arg;
    }

    /// <summary>
    /// NPC对话包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    public unsafe struct DOWN_NpcYell
    {
        [FieldOffset(0x00)] public ulong ActorId;
        [FieldOffset(0x08)] public uint NameId;
        [FieldOffset(0x0C)] public ushort NpcYellId;
        [FieldOffset(0x10)] public fixed int Args[4];
    }

    /// <summary>
    /// 队伍成员信息结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x1C0)]
    public unsafe struct PartyMember
    {
        [FieldOffset(0x00)] public fixed byte NameBytes[32];
        [FieldOffset(0x20)] public ulong AccountId;
        [FieldOffset(0x28)] public ulong CharacterId;
        [FieldOffset(0x30)] public uint ActorId;
        [FieldOffset(0x34)] public uint PetId;
        [FieldOffset(0x38)] public uint BuddyId;
        [FieldOffset(0x3C)] public uint CurrentHp;
        [FieldOffset(0x40)] public uint MaxHp;
        [FieldOffset(0x44)] public ushort CurrentMp;
        [FieldOffset(0x46)] public ushort MaxMp;
        [FieldOffset(0x48)] public ushort HomeWorldId;
        [FieldOffset(0x4A)] public ushort TerritoryId;
        [FieldOffset(0x4C)] public byte Flag;
        [FieldOffset(0x4D)] public byte ClassJob;
        [FieldOffset(0x4E)] public byte Sex;
        [FieldOffset(0x4F)] public byte Level;
        [FieldOffset(0x50)] public byte LevelSync;
        [FieldOffset(0x51)] public byte PlatformType;
        [FieldOffset(0x54)] public fixed byte Status[360]; // 30 个 Status
    }

    /// <summary>
    /// 队伍更新包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0xE18)]
    public unsafe struct DOWN_PartyUpdate
    {
        [FieldOffset(0x00)] public fixed byte Members[3584]; // 8 个 PartyMember (8 * 0x1C0)
        [FieldOffset(0xE00)] public ulong PartyId;
        [FieldOffset(0xE08)] public ulong ChatChannel;
        [FieldOffset(0xE10)] public byte LeaderIndex;
        [FieldOffset(0xE11)] public byte PartyCount;
    }

    /// <summary>
    /// 批量开始动作时间线包结构体
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    public unsafe struct DOWN_StartActionTimelineMulti
    {
        [FieldOffset(0x00)] public fixed uint Ids[10];
        [FieldOffset(0x28)] public fixed ushort TimelineIds[10];
    }

    // 在这里添加更多 DOWN 包结构体
}
