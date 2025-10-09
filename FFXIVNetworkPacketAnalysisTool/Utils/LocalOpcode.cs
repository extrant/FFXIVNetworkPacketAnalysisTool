using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Memory;
using Serilog;

namespace FFXIVNetworkPacketAnalysisTool.Utils;


/// <summary>
/// 在线opcode没有记录的 扫描sig加进入吧
/// </summary>
public static class LocalOpcode
{
    // 潜水艇TP的
    [Opcode(name:"UP_DiveStart",offset:0x4)]
    private static readonly CompSig DiveStartOpcodeBaseSig =
        new("C7 44 24 ?? ?? ?? ?? ?? 48 C7 44 24 ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? B0");

    public static void SetLocalUpOpcode(Dictionary<int, string> opcodes)
    {
        var type = typeof(LocalOpcode);
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (field.FieldType != typeof(CompSig)) continue;
            var value = field.GetValue(null);
            if (value is CompSig sig)
            {
                var attr = field.GetCustomAttribute<OpcodeAttribute>();
                if (attr != null)
                {
                    var sanSig = SanSig(sig,attr.Offset);
                    if (sanSig ==0)continue;
                    if (!opcodes.TryGetValue(sanSig, out var opcode))
                    {
                        Log.Debug($"添加不到，尝试添加{sanSig},{attr.Name}");
                        opcodes.TryAdd(sanSig,attr.Name);
                    }
                }
            }
        }
    }

    private static int SanSig(CompSig sig ,int offset)
    {
        var data = sig.ScanText();
        return data == nint.Zero ? 0 : MemoryHelper.Read<int>(data + offset);
    }
}
[AttributeUsage(AttributeTargets.Field)]
public class OpcodeAttribute: Attribute
{
    public string Name { get; }
    public int Offset { get; }

    public OpcodeAttribute(string name, int offset)
    {
        Name = name;
        Offset = offset;
    }
}
