using Dalamud.Interface.Windowing;
using FFXIVNetworkPacketAnalysisTool.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace FFXIVNetworkPacketAnalysisTool.Windows;

public class PacketResendWindow : Window, IDisposable
{
    private Plugin Plugin;
    private PacketInfo OriginalPacket;
    private Type? StructType;
    private byte[] EditableData;
    private Dictionary<string, object> FieldValues = new Dictionary<string, object>();
    private Dictionary<string, string> FieldInputs = new Dictionary<string, string>();
    private const int PACKET_HEADER_SIZE = 0x20;

    public PacketResendWindow(Plugin plugin, PacketInfo packet, Type? structType)
        : base($"重发包 - {packet.OpcodeName}###ResendPacket{packet.Timestamp.Ticks}")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        OriginalPacket = packet;
        StructType = structType;

        // 复制原始数据
        EditableData = new byte[packet.RawData.Length];
        Array.Copy(packet.RawData, EditableData, packet.RawData.Length);

        // 初始化字段值
        if (StructType != null)
        {
            InitializeFieldValues();
        }

        IsOpen = true;
    }

    public void Dispose()
    {
        // 从窗口系统中移除
        Plugin.WindowSystem.RemoveWindow(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        // 窗口关闭时自动清理
        Dispose();
    }

    private void InitializeFieldValues()
    {
        if (StructType == null) return;

        var fields = StructType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() != null)
            .ToList();

        int headerOffset = OriginalPacket.Direction == PacketDirection.Send ? PACKET_HEADER_SIZE : PACKET_HEADER_SIZE;

        foreach (var field in fields)
        {
            var offsetAttr = field.GetCustomAttribute<FieldOffsetAttribute>();
            if (offsetAttr == null) continue;

            int offset = offsetAttr.Value + headerOffset;

            try
            {
                var value = ReadFieldValue(field.FieldType, EditableData, offset);
                FieldValues[field.Name] = value;
                FieldInputs[field.Name] = FormatValueForInput(value, field.FieldType);
            }
            catch
            {
                FieldValues[field.Name] = GetDefaultValue(field.FieldType);
                FieldInputs[field.Name] = "";
            }
        }
    }

    private string FormatValueForInput(object value, Type type)
    {
        if (type.IsEnum)
        {
            // 枚举类型：存储数值，而不是名称
            var underlyingType = Enum.GetUnderlyingType(type);
            var numericValue = Convert.ChangeType(value, underlyingType);
            return $"0x{numericValue:X}";
        }
        else if (IsComplexType(type))
        {
            // Vector3 等复杂类型不需要字符串表示（使用拖拽条直接编辑）
            return "";
        }
        else if (type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong))
        {
            return $"0x{value:X}";
        }
        else
        {
            return value.ToString() ?? "";
        }
    }

    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }
        return 0;
    }

    public override void Draw()
    {
        ImGui.Text($"原始包: {OriginalPacket.OpcodeName} (0x{OriginalPacket.Opcode:X4})");
        ImGui.Text($"方向: {OriginalPacket.DirectionString}");
        ImGui.Text($"时间: {OriginalPacket.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
        ImGui.Separator();

        if (StructType == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), "未找到对应的结构体定义，无法编辑字段");
            ImGui.TextWrapped("只能重发原始数据。");
            ImGui.Separator();

            if (ImGui.Button("重发原始包"))
            {
                ResendPacket();
            }
        }
        else
        {
            DrawFieldEditor();
        }

        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.Button("关闭"))
        {
            IsOpen = false;
        }
    }

    private void DrawFieldEditor()
    {
        if (StructType == null) return;

        ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1f), $"结构体: {StructType.Name}");
        ImGui.Text("编辑字段值后点击\"应用并重发\"");
        ImGui.Separator();

        var fields = StructType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() != null)
            .OrderBy(f => f.GetCustomAttribute<FieldOffsetAttribute>()!.Value)
            .ToList();

        ImGui.BeginChild("FieldEditorChild", new Vector2(0, -40), true);

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 4f));

        if (ImGui.BeginTable("FieldEditor", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("偏移", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("字段名", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("类型", ImGuiTableColumnFlags.WidthFixed, 120f);
            ImGui.TableSetupColumn("值", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var field in fields)
            {
                var offsetAttr = field.GetCustomAttribute<FieldOffsetAttribute>();
                if (offsetAttr == null) continue;

                int offset = offsetAttr.Value;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"0x{offset:X2}");

                ImGui.TableNextColumn();
                ImGui.Text(field.Name);

                ImGui.TableNextColumn();
                ImGui.Text(field.FieldType.Name);

                ImGui.TableNextColumn();
                DrawFieldInput(field);
            }

            ImGui.EndTable();
        }

        ImGui.PopStyleVar(2);
        ImGui.EndChild();

        if (ImGui.Button("应用并重发"))
        {
            if (ApplyFieldChanges())
            {
                ResendPacket();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("重置为原始值"))
        {
            Array.Copy(OriginalPacket.RawData, EditableData, OriginalPacket.RawData.Length);
            InitializeFieldValues();
        }
    }

    private void DrawFieldInput(FieldInfo field)
    {
        if (!FieldInputs.ContainsKey(field.Name))
            return;

        ImGui.PushID(field.Name);

        if (field.FieldType.IsEnum)
        {
            // 枚举类型：使用下拉框
            var currentValue = FieldValues[field.Name];
            var enumValues = Enum.GetValues(field.FieldType);
            var enumNames = Enum.GetNames(field.FieldType);

            int currentIndex = Array.IndexOf(enumValues, currentValue);
            if (currentIndex < 0) currentIndex = 0;

            var previewValue = $"{enumNames[currentIndex]} ({currentValue})";

            if (ImGui.BeginCombo("##enum", previewValue))
            {
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bool isSelected = i == currentIndex;
                    var enumValue = enumValues.GetValue(i);
                    var displayText = $"{enumNames[i]} ({enumValue})";

                    if (ImGui.Selectable(displayText, isSelected))
                    {
                        FieldValues[field.Name] = enumValue!;
                        FieldInputs[field.Name] = enumValue!.ToString()!;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

        }
        else if (field.FieldType == typeof(Vector3))
        {
            // Vector3 类型：使用三个浮点数输入框
            var vec3 = (Vector3)FieldValues[field.Name];
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("##x", ref vec3.X, 0.1f, float.MinValue, float.MaxValue, "X: %.3f"))
            {
                FieldValues[field.Name] = vec3;
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("##y", ref vec3.Y, 0.1f, float.MinValue, float.MaxValue, "Y: %.3f"))
            {
                FieldValues[field.Name] = vec3;
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragFloat("##z", ref vec3.Z, 0.1f, float.MinValue, float.MaxValue, "Z: %.3f"))
            {
                FieldValues[field.Name] = vec3;
            }
        }
        else if (IsFixedArray(field.FieldType))
        {
            // 固定数组：显示但不允许编辑（暂时）
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "[固定数组 - 暂不支持编辑]");
        }
        else
        {
            // 基本类型：使用输入框
            ImGui.SetNextItemWidth(-1);
            var input = FieldInputs[field.Name];
            if (ImGui.InputText("##input", ref input, 64))
            {
                FieldInputs[field.Name] = input;
            }
        }

        ImGui.PopID();
    }

    private bool IsFixedArray(Type type)
    {
        return type.Name.Contains("<") || type.IsNestedPublic;
    }

    private bool IsComplexType(Type type)
    {
        // Vector3 等复杂类型
        return type == typeof(Vector3) || type == typeof(Vector2) || type == typeof(Vector4);
    }

    private bool ApplyFieldChanges()
    {
        if (StructType == null) return false;

        var fields = StructType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() != null)
            .ToList();

        int headerOffset = OriginalPacket.Direction == PacketDirection.Send ? PACKET_HEADER_SIZE : PACKET_HEADER_SIZE;

        foreach (var field in fields)
        {
            if (IsFixedArray(field.FieldType))
                continue; // 跳过固定数组

            var offsetAttr = field.GetCustomAttribute<FieldOffsetAttribute>();
            if (offsetAttr == null) continue;

            int offset = offsetAttr.Value + headerOffset;

            try
            {
                object value;

                if (field.FieldType.IsEnum)
                {
                    // 尝试从手动输入解析，如果失败则使用当前选中值
                    var input = FieldInputs[field.Name];
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        var underlyingType = Enum.GetUnderlyingType(field.FieldType);
                        var numericValue = ParseNumericValue(input, underlyingType);
                        value = Enum.ToObject(field.FieldType, numericValue);
                    }
                    else
                    {
                        value = FieldValues[field.Name];
                    }
                }
                else if (IsComplexType(field.FieldType))
                {
                    // Vector3 等复杂类型直接从 FieldValues 获取（已在 UI 中更新）
                    value = FieldValues[field.Name];
                }
                else
                {
                    var input = FieldInputs[field.Name];
                    value = ParseNumericValue(input, field.FieldType);
                }

                WriteFieldValue(field.FieldType, EditableData, offset, value);
                FieldValues[field.Name] = value;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"应用字段 {field.Name} 时出错: {ex.Message}");
                ImGui.OpenPopup("错误");
                return false;
            }
        }

        return true;
    }

    private object ParseNumericValue(string input, Type targetType)
    {
        input = input.Trim();

        // 支持十六进制
        bool isHex = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (isHex)
            input = input.Substring(2);

        var numberStyle = isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer;

        if (targetType == typeof(byte))
            return byte.Parse(input, numberStyle);
        else if (targetType == typeof(ushort))
            return ushort.Parse(input, numberStyle);
        else if (targetType == typeof(uint))
            return uint.Parse(input, numberStyle);
        else if (targetType == typeof(ulong))
            return ulong.Parse(input, numberStyle);
        else if (targetType == typeof(short))
            return short.Parse(input, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);
        else if (targetType == typeof(int))
            return int.Parse(input, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);
        else if (targetType == typeof(long))
            return long.Parse(input, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer);
        else if (targetType == typeof(float))
            return float.Parse(input);
        else if (targetType == typeof(double))
            return double.Parse(input);

        throw new NotSupportedException($"不支持的类型: {targetType.Name}");
    }

    private unsafe void ResendPacket()
    {
        try
        {
            // 注意：只支持发包（客户端->服务器）
            if (OriginalPacket.Direction != PacketDirection.Send)
            {
                Plugin.Log.Warning("只能重发发包（客户端->服务器）");
                return;
            }

            // 直接使用反射调用底层 SendPacket 委托
            // SendPacket 的签名: bool SendPacket(NetworkModuleProxy* module, byte* packet, uint a3, uint a4)
            var sendPacketField = typeof(NetRe).GetField("SendPacket", BindingFlags.NonPublic | BindingFlags.Static);
            if (sendPacketField == null)
            {
                Plugin.Log.Error("无法找到 SendPacket 委托");
                return;
            }

            var sendPacketDelegate = sendPacketField.GetValue(null);
            if (sendPacketDelegate == null)
            {
                Plugin.Log.Error("SendPacket 委托未初始化");
                return;
            }

            // 获取 NetworkModuleProxy
            var frameworkInstance = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            if (frameworkInstance == null || frameworkInstance->NetworkModuleProxy == null)
            {
                Plugin.Log.Error("NetworkModuleProxy 未初始化");
                return;
            }

            var networkModule = frameworkInstance->NetworkModuleProxy;

            // 准备要发送的数据
            // EditableData 包含完整的包（包括包头0x20字节），直接使用
            fixed (byte* packetPtr = EditableData)
            {
                // 调用 SendPacket
                var invokeMethod = sendPacketDelegate.GetType().GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var parameters = new object[] { (IntPtr)networkModule, (IntPtr)packetPtr, (uint)0, (uint)0x114514 };
                    var result = invokeMethod.Invoke(sendPacketDelegate, parameters);

                    Plugin.Log.Info($"已重发包: {OriginalPacket.OpcodeName} (0x{OriginalPacket.Opcode:X4})");

                    // 关闭窗口
                    IsOpen = false;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"重发包时出错: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private object ReadFieldValue(Type type, byte[] data, int offset)
    {
        if (offset >= data.Length)
            throw new IndexOutOfRangeException("偏移超出数据范围");

        var method = typeof(MainWindow.StructConverter)
            .GetMethod(nameof(MainWindow.StructConverter.FromBytesGeneric), BindingFlags.Static | BindingFlags.Public)
            ?.MakeGenericMethod(type);

        return method?.Invoke(null, new object[] { data, offset }) ?? throw new InvalidOperationException("无法读取字段值");
    }

    private unsafe void WriteFieldValue(Type type, byte[] data, int offset, object value)
    {
        if (offset >= data.Length)
            throw new IndexOutOfRangeException("偏移超出数据范围");

        fixed (byte* ptr = &data[offset])
        {
            if (type == typeof(byte))
                *ptr = (byte)value;
            else if (type == typeof(ushort))
                *(ushort*)ptr = (ushort)value;
            else if (type == typeof(uint))
                *(uint*)ptr = (uint)value;
            else if (type == typeof(ulong))
                *(ulong*)ptr = (ulong)value;
            else if (type == typeof(short))
                *(short*)ptr = (short)value;
            else if (type == typeof(int))
                *(int*)ptr = (int)value;
            else if (type == typeof(long))
                *(long*)ptr = (long)value;
            else if (type == typeof(float))
                *(float*)ptr = (float)value;
            else if (type == typeof(double))
                *(double*)ptr = (double)value;
            else if (type == typeof(Vector3))
            {
                var vec3 = (Vector3)value;
                *(float*)ptr = vec3.X;
                *(float*)(ptr + 4) = vec3.Y;
                *(float*)(ptr + 8) = vec3.Z;
            }
            else if (type == typeof(Vector2))
            {
                var vec2 = (Vector2)value;
                *(float*)ptr = vec2.X;
                *(float*)(ptr + 4) = vec2.Y;
            }
            else if (type == typeof(Vector4))
            {
                var vec4 = (Vector4)value;
                *(float*)ptr = vec4.X;
                *(float*)(ptr + 4) = vec4.Y;
                *(float*)(ptr + 8) = vec4.Z;
                *(float*)(ptr + 12) = vec4.W;
            }
            else if (type.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(type);
                var converted = Convert.ChangeType(value, underlyingType);
                WriteFieldValue(underlyingType, data, offset, converted);
            }
            else
                throw new NotSupportedException($"不支持的类型: {type.Name}");
        }
    }
}
