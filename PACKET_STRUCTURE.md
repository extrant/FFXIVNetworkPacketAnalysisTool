# FFXIV 网络包结构说明

## 收包 (DOWN) 包头结构

```
偏移   大小   类型     说明
--------------------------------------
0x00   4     uint32   包总长度 (含包头)
0x04   4     uint32   未知/保留
0x08   8     uint64   时间戳
0x10   2     ushort   未知
0x12   2     ushort   Opcode
0x14   2     ushort   未知
0x16   2     ushort   ServerID
0x18   ...   bytes    包体数据
```

## 发包 (UP) 包头结构

```
偏移   大小   类型     说明
--------------------------------------
0x00   2     ushort   Opcode
0x02   2     ushort   未知
0x04   4     uint32   未知
0x08   4     uint32   包长度
0x0C   ...   bytes    包体数据
```

## 当前实现 - 基于结构体定义的包长度获取 ✨

### 收包长度读取（智能方式）
- **方法**: `GetPacketLengthFromStruct(string opcodeName)`
- **位置**: `NetManager.cs` 第 227-287 行
- **工作流程**:
  1. 根据 OpcodeName 查找对应的结构体定义
  2. 如果找到结构体，读取 `[StructLayout(LayoutKind.Explicit, Size = ...)]` 中的 `Size` 属性
  3. 如果未找到结构体或 OpcodeName 为 "Unknown"，返回默认值 512 字节
  4. 使用 `ConcurrentDictionary` 缓存已查找的结果，避免重复反射

**优势**:
- ✅ 每个包都能获得精确的长度
- ✅ 只需定义结构体，无需硬编码每个包的长度
- ✅ 自动识别新添加的结构体定义
- ✅ 高性能缓存机制

### 发包长度读取
- **位置**: `NetManager.cs` 第 109 行
- **偏移**: `0x08` (packet + 8)
- **读取方式**: `*(uint*)(packet + 8)`

## 如何为包定义结构体

在 `PacketStructures/ExampleStructs.cs` 或其他文件中定义结构体：

```csharp
[StructLayout(LayoutKind.Explicit, Size = 0x290)]  // 👈 这个 Size 会被用作包长度！
public unsafe struct DOWN_NpcSpawn
{
    [FieldOffset(0x10)] public ulong MainTarget;
    [FieldOffset(0x40)] public uint DataID;
    [FieldOffset(0x44)] public uint OwnerID;
    // ... 其他字段
}
```

**重要规则**:
1. **结构体名称** 必须与 OpcodeName 完全匹配
   - 例如: `DOWN_NpcSpawn`, `DOWN_PlayerSpawn`, `UP_ClientTrigger`
2. **必须包含** `[StructLayout(LayoutKind.Explicit, Size = ...)]` 特性
3. **Size 参数** 指定包的总大小（字节）
4. 定义后会**自动被识别**，无需额外配置

## 注意事项

1. **包头偏移调整**: 在 `ReceivePacketInternalDetour` 中，`packet -= 16` 是因为原始指针指向包体，需要回退到包头起始位置

2. **自动长度获取**: 系统会自动从结构体定义中获取长度，不再需要手动从包头读取

3. **最大限制**: 在 `CapturePacket` 中限制最大复制 4KB 数据，防止超大包占用过多内存

4. **包头结构体示例**: 如果需要定义包头结构体，可以参考：

```csharp
[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct FFXIVPacketHeader
{
    [FieldOffset(0x00)] public uint Length;        // 包总长度
    [FieldOffset(0x04)] public uint Reserved;
    [FieldOffset(0x08)] public ulong Timestamp;
    [FieldOffset(0x10)] public ushort Unknown1;
    [FieldOffset(0x12)] public ushort Opcode;
    [FieldOffset(0x14)] public ushort Unknown2;
    [FieldOffset(0x16)] public ushort ServerID;
}
```

## 调试技巧

如果长度读取仍然不准确：

1. 在 `ReceivePacketInternalDetour` 中添加日志：
```csharp
Plugin.Log.Debug($"包长度: offset0={Marshal.ReadInt32((nint)packet, 0)}, " +
                 $"offset24={Marshal.ReadInt32((nint)packet, 24)}, " +
                 $"offset8={Marshal.ReadInt32((nint)packet, 8)}");
```

2. 使用十六进制查看器检查实际包数据，确认长度字段位置

3. 参考其他 FFXIV 网络插件的实现（如 Machina, FFXIVMon）
