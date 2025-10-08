using Dalamud;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVNetworkPacketAnalysisTool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVNetworkPacketAnalysisTool.Utils
{

    public unsafe class NetRe : IDisposable
    {
        private Configuration _configuration;

        // 包捕获队列 - 线程安全
        public ConcurrentQueue<PacketInfo> PacketQueue { get; } = new ConcurrentQueue<PacketInfo>();

        // 控制是否启用捕获
        public bool CaptureEnabled { get; set; } = true;

        // 最大队列大小，防止内存溢出
        private const int MaxQueueSize = 10000;

        // 包长度缓存 - 避免重复反射查找
        private ConcurrentDictionary<string, int> _packetLengthCache = new ConcurrentDictionary<string, int>();

        private static readonly CompSig SendPacketInternalSig =
            new("48 83 EC ?? 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 ?? 44 89 44 24 ?? 4C 8D 44 24 ?? 44 89 4C 24 ?? 44 0F B6 4C 24");
        private delegate void* SendPacketInternalDelegate(NetworkModuleProxy* module, byte* packet, int a3, int a4, ushort priority);
        private static Hook<SendPacketInternalDelegate>? SendPacketInternalHook;

        private delegate void ReceivePacketInternalDelegate(PacketDispatcher* dispatcher, uint targetID, byte* packet);
        private static Hook<ReceivePacketInternalDelegate>? ReceivePacketInternalHook;

        private delegate bool SendPacketDelegate(NetworkModuleProxy* module, byte* packet, uint a3, uint a4);
        private static readonly SendPacketDelegate? SendPacket = new CompSig("E8 ?? ?? ?? ?? 48 8B D6 48 8B CF E8 ?? ?? ?? ?? 48 8B 8C 24").GetDelegate<SendPacketDelegate>();

        public delegate void PreSendPacketDelegate(ref bool isPrevented, int opcode, ref byte* packet, ref ushort priority);
        public delegate void PostSendPacketDelegate(int opcode, byte* packet, ushort priority);

        public delegate void PreReceivePacketDelegate(ref bool isPrevented, int opcode, ref byte* packet);
        public delegate void PostReceivePacketDelegate(int opcode, byte* packet);

        public uint UpdatePositionInstance = 0;
        public uint UpdatePositionHandler = 0;

        public unsafe nint GetVFuncByName<T>(T* vtablePtr, string fieldName) where T : unmanaged
        {
            var vtType = typeof(T);
            var fi = vtType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null)
                throw new MissingFieldException(vtType.FullName, fieldName);

            var offAttr = fi.GetCustomAttribute<FieldOffsetAttribute>();
            if (offAttr == null)
                throw new InvalidOperationException($"Field {fieldName} has no FieldOffset");

            var offset = offAttr.Value;

            return *(nint*)((byte*)vtablePtr + offset);
        }

        public NetRe(Configuration configuration)
        {
            _configuration = configuration;
            SendPacketInternalHook ??= SendPacketInternalSig.GetHook<SendPacketInternalDelegate>(SendPacketInternalDetour);
            SendPacketInternalHook.Enable();

            ReceivePacketInternalHook ??=
                Plugin.Hook.HookFromAddress<ReceivePacketInternalDelegate>(GetVFuncByName(PacketDispatcher.StaticVirtualTablePointer, "OnReceivePacket"),
                                                                             ReceivePacketInternalDetour);
            ReceivePacketInternalHook.Enable();


            Plugin.Log.Debug("FFXIVNetworkPacketAnalysisTool Successfully Loaded.");
        }

        public void Dispose()
        {
            SendPacketInternalHook?.Dispose();
            SendPacketInternalHook = null;

            ReceivePacketInternalHook?.Dispose();
            ReceivePacketInternalHook = null;
            Plugin.Log.Debug("FFXIVNetworkPacketAnalysisTool Successfully Uninstalled.");
        }

        public interface IGamePacket
        {
            public string Log();

            public void Send();
        }

        public void SendPackt<T>(T data) where T : unmanaged, IGamePacket =>
            SendPacket(Framework.Instance()->NetworkModuleProxy, (byte*)&data, 0, 0x9876543); // 打个标记

        private void* SendPacketInternalDetour(NetworkModuleProxy* module, byte* packet, int a3, int a4, ushort priority)
        {
            // 优先让原始数据通过，减少游戏延迟
            var orginal = SendPacketInternalHook.Original(module, packet, a3, a4, priority);

            // 在原始调用之后再进行捕获和日志（异步处理，不阻塞游戏）
            if (CaptureEnabled)
            {
                var opcode = *(ushort*)packet;
                var opcodeName = GetUpOpcodeName(opcode);

                // 尝试从结构体定义中获取包长度
                // 发包包体前有 0x10 (16) 字节的包头，结构体长度需要加上这个偏移
                var structLength = *(uint*)(packet + 8);//GetPacketLengthFromStruct(opcodeName);
                var length = structLength + 0x20; //加上包头长度

                // 快速日志（可选，如果影响性能可以注释掉）
                // LogKnownGamePacket(opcode, packet, priority);

                // 捕获包数据到队列
                CapturePacket(opcode, packet, (int)length, priority, PacketDirection.Send);
            }

            return orginal;
        }

        #region 收包处理

        private void ReceivePacketInternalDetour(PacketDispatcher* dispatcher, uint targetID, byte* packet)
        {
            packet -= 16;

            // 优先让原始数据通过，减少游戏延迟
            ReceivePacketInternalHook.Original(dispatcher, targetID, packet + 16);

            // 在原始调用之后再进行捕获和日志（异步处理，不阻塞游戏）
            if (CaptureEnabled)
            {
                var opcode = Marshal.ReadInt16((nint)packet, 18);
                var opcodeName = GetDownOpcodeName((ushort)opcode);

                // 尝试从结构体定义中获取包长度
                // 收包包体前有 0x20 (32) 字节的包头，结构体长度需要加上这个偏移
                var structLength = GetPacketLengthFromStruct(opcodeName);
                var length = structLength + 0x20; // 加上包头长度

                // 快速日志（可选，如果影响性能可以注释掉）
                // LogKnownReceivedPacket((ushort)opcode, targetID);

                // 捕获包数据到队列
                CapturePacket((ushort)opcode, packet, length, 0, PacketDirection.Receive, targetID);
            }
        }

        #endregion


        // 1. 用两个新的、独立的函数替换旧的 GetOpcodeSubCategoryName 函数

        private string GetUpOpcodeName(ushort opcode)
        {
            // 直接在 UpOpcodes 字典中查找，高效且准确
            if (_configuration.UpOpcodes.TryGetValue(opcode, out var name))
            {
                return name;
            }
            return "Unknown";
        }

        private string GetDownOpcodeName(ushort opcode)
        {
            // 直接在 DownOpcodes 字典中查找，高效且准确
            if (_configuration.DownOpcodes.TryGetValue(opcode, out var name))
            {
                return name;
            }
            return "Unknown";
        }


        // 2. 修改日志函数，让它们分别调用对应的查找函数

        private void LogKnownGamePacket(ushort opcode, byte* packet, ushort priority) // 这是发包 (UP)
        {
                // 调用 GetUpOpcodeName
                //Plugin.Log.Debug($"[I-Ching-Net] Opcode [发包] Opcode: {opcode}->{GetUpOpcodeName(opcode)} / 长度: {*(uint*)(packet + 8)} / 优先级: {priority}");
        }

        private void LogKnownReceivedPacket(ushort opcode, uint targetID) // 这是收包 (DOWN)
        {
                // 调用 GetDownOpcodeName
                //Plugin.Log.Debug($"[I-Ching-Net] Opcode [收包] Opcode: {opcode}->{GetDownOpcodeName(opcode)} / 目标: 0x{targetID:X8}");
        }

        // 捕获包数据到队列（优化版：最小化性能影响）
        private void CapturePacket(ushort opcode, byte* packet, int length, ushort priority, PacketDirection direction, uint targetID = 0)
        {
            try
            {
                // 快速检查队列是否已满，避免过多的入队操作
                if (PacketQueue.Count >= MaxQueueSize)
                {
                    // 队列满了就直接丢弃，不阻塞
                    return;
                }

                // 限制复制的数据大小，减少内存分配
                int captureLength = Math.Min(length, 2048); // 降低到2KB以提高性能

                var packetInfo = new PacketInfo
                {
                    SessionId = DateTime.Now.Ticks,
                    Timestamp = DateTime.Now,
                    Direction = direction,
                    Opcode = opcode,
                    OpcodeName = direction == PacketDirection.Send ? GetUpOpcodeName(opcode) : GetDownOpcodeName(opcode),
                    RawData = PacketInfo.CloneData(packet, captureLength), // 快速复制
                    PacketLength = (uint)length, // 记录实际长度
                    Priority = priority,
                    TargetID = targetID
                };

                PacketQueue.Enqueue(packetInfo);
            }
            catch
            {
                // 静默失败，不影响游戏性能
            }
        }

        /// <summary>
        /// 从结构体定义中获取包长度
        /// 如果找到对应的结构体，返回结构体的 Size；否则返回默认值 512
        /// </summary>
        private int GetPacketLengthFromStruct(string opcodeName)
        {
            // 检查缓存
            if (_packetLengthCache.TryGetValue(opcodeName, out var cachedLength))
            {
                return cachedLength;
            }

            // Unknown 的包直接返回默认值
            if (opcodeName == "Unknown")
            {
                _packetLengthCache[opcodeName] = 512;
                return 512;
            }

            try
            {
                // 在当前程序集中查找对应的结构体
                var assembly = Assembly.GetExecutingAssembly();
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // 检查类型名称是否匹配，且是值类型（struct）
                    if (type.Name == opcodeName && type.IsValueType)
                    {
                        // 获取 StructLayout 特性
                        var structLayoutAttr = type.GetCustomAttribute<StructLayoutAttribute>();
                        if (structLayoutAttr != null && structLayoutAttr.Size > 0)
                        {
                            int size = structLayoutAttr.Size;
                            _packetLengthCache[opcodeName] = size;
                            Plugin.Log.Debug($"[包长度] 从结构体 {opcodeName} 获取长度: {size} 字节");
                            return size;
                        }

                        // 如果没有显式指定 Size，使用 Marshal.SizeOf
                        try
                        {
                            int size = Marshal.SizeOf(type);
                            _packetLengthCache[opcodeName] = size;
                            Plugin.Log.Debug($"[包长度] 从结构体 {opcodeName} 计算长度: {size} 字节");
                            return size;
                        }
                        catch
                        {
                            // Marshal.SizeOf 可能失败（例如包含托管类型）
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"查找结构体 {opcodeName} 时出错: {ex.Message}");
            }

            // 未找到结构体定义，使用默认值
            _packetLengthCache[opcodeName] = 512;
            return 512;
        }

    }


}
