using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using FFXIVNetworkPacketAnalysisTool.Utils;
using FFXIVNetworkPacketAnalysisTool.PacketStructures;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FFXIVNetworkPacketAnalysisTool.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private NetRe NetRe => Plugin.MyNetRe;

    // 会话管理
    private List<PacketSession> _sessions = new List<PacketSession>();
    private PacketSession _currentSession;
    private int _sessionCounter = 1;
    private long _pendingSessionSwitch = 0; // 待切换的会话ID

    // 控制状态
    private bool _isPaused = false;

    // 选中的包 - 支持多选
    private PacketInfo? _selectedPacket = null;
    private int _selectedIndex = -1;
    private HashSet<int> _selectedIndices = new HashSet<int>(); // 多选支持
    private int _lastClickedIndex = -1; // Shift多选的起点

    // UI 布局
    private float _leftPanelWidth = 600f;
    private float _splitterWidth = 8f;
    private bool _isDraggingSplitter = false;

    // 过滤器
    private string _filterText = "";

    // 缓存限制（从配置读取）
    private int MaxPacketsInSession => Plugin.Configuration.MaxPacketsPerSession;

    // 包体结构体解析缓存
    private Dictionary<string, Type?> _structTypeCache = new Dictionary<string, Type?>();

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("FFXIV NPATool###FFXIVNetworkPacketAnalysisTool")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(900, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        // 创建第一个会话
        _currentSession = new PacketSession(DateTime.Now.Ticks, $"会话 {_sessionCounter}");
        _sessions.Add(_currentSession);

        // 注意：不在构造函数中加载配置，因为 NetRe 可能还未初始化
        // 配置会在第一次 Draw() 时加载
    }

    public void Dispose()
    {
        // 保存设置到配置
        SaveSettingsToConfig();

        // 清理所有会话
        foreach (var session in _sessions)
        {
            session.Dispose();
        }
        _sessions.Clear();
    }

    private bool _settingsLoaded = false;

    private void LoadSettingsFromConfig()
    {
        if (_settingsLoaded || Plugin.MyNetRe == null)
            return;

        var config = Plugin.Configuration;
        Plugin.MyNetRe.CaptureEnabled = config.CaptureEnabled;
        _settingsLoaded = true;
    }

    private void SaveSettingsToConfig()
    {
        if (Plugin.MyNetRe == null)
            return;

        // 配置已经在使用时实时保存，这里只需要确保最终保存
        Plugin.Configuration.CaptureEnabled = Plugin.MyNetRe.CaptureEnabled;
        Plugin.Configuration.Save();
    }

    public override void Draw()
    {
        // 首次运行时加载配置
        LoadSettingsFromConfig();

        // 从队列中获取新包
        if (!_isPaused && _currentSession.IsActive)
        {
            ProcessIncomingPackets();
        }

        // 绘制会话标签页
        DrawSessionTabs();
        ImGui.Separator();

        DrawControlPanel();
        ImGui.Separator();

        // 分栏布局：左侧包列表 + 右侧详细信息
        DrawSplitView();
    }

    private void ProcessIncomingPackets()
    {
        if (Plugin.MyNetRe == null)
            return;

        int processedCount = 0;
        while (Plugin.MyNetRe.PacketQueue.TryDequeue(out var packet) && processedCount < 100) // 每帧最多处理100个包
        {
            packet.SessionId = _currentSession.SessionId;
            _currentSession.Packets.Add(packet);
            processedCount++;

            // 限制缓存大小
            if (_currentSession.Packets.Count > MaxPacketsInSession)
            {
                _currentSession.Packets.RemoveAt(0);
                if (_selectedIndex >= 0)
                    _selectedIndex--;
            }
        }
    }

    private void DrawSessionTabs()
    {
        ImGui.Text("会话标签:");
        ImGui.SameLine();

        // 使用 AutoSelectTabToFitWidth 让标签页自动调整宽度
        if (ImGui.BeginTabBar("SessionTabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.AutoSelectNewTabs))
        {
            for (int i = 0; i < _sessions.Count; i++)
            {
                var session = _sessions[i];

                // 活跃会话不显示关闭按钮
                bool isOpen = !session.IsActive;

                var tabFlags = ImGuiTabItemFlags.None;
                if (session.IsActive)
                    tabFlags |= ImGuiTabItemFlags.UnsavedDocument; // 活跃会话显示小点

                // 如果需要切换到这个会话，设置 SetSelected 标志
                if (_pendingSessionSwitch == session.SessionId)
                {
                    tabFlags |= ImGuiTabItemFlags.SetSelected;
                    _pendingSessionSwitch = 0; // 清除待切换标志
                }

                // 活跃会话使用 NoCloseWithMiddleMouseButton 防止中键关闭
                if (session.IsActive)
                    tabFlags |= ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;

                bool hasCloseButton = !session.IsActive;

                if (hasCloseButton)
                {
                    if (ImGui.BeginTabItem($"{session.GetDisplayName()}###Session{session.SessionId}", ref isOpen, tabFlags))
                    {
                        // 切换到这个会话
                        if (_currentSession != session)
                        {
                            _currentSession = session;
                            _selectedPacket = null;
                            _selectedIndex = -1;
                            _selectedIndices.Clear();
                            _lastClickedIndex = -1;
                        }

                        ImGui.EndTabItem();
                    }
                }
                else
                {
                    // 活跃会话不带关闭按钮
                    if (ImGui.BeginTabItem($"{session.GetDisplayName()}###Session{session.SessionId}", tabFlags))
                    {
                        // 切换到这个会话
                        if (_currentSession != session)
                        {
                            _currentSession = session;
                            _selectedPacket = null;
                            _selectedIndex = -1;
                            _selectedIndices.Clear();
                            _lastClickedIndex = -1;
                        }

                        ImGui.EndTabItem();
                    }
                }

                // 用户点击关闭按钮（只有非活跃会话才会触发）
                if (!isOpen && !session.IsActive)
                {
                    // 如果只剩一个会话，不允许关闭
                    if (_sessions.Count > 1)
                    {
                        // 如果关闭的是当前会话，切换到其他会话
                        if (_currentSession == session)
                        {
                            int nextIndex = i > 0 ? i - 1 : 1;
                            _currentSession = _sessions[nextIndex];
                            _selectedPacket = null;
                            _selectedIndex = -1;
                            _selectedIndices.Clear();
                            _lastClickedIndex = -1;
                        }

                        // 清理并删除会话
                        session.Dispose();
                        _sessions.RemoveAt(i);
                        break; // 退出循环，因为列表已被修改
                    }
                }
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawControlPanel()
    {
        ImGui.Text("控制面板");
        ImGui.SameLine();

        // 暂停/继续按钮
        if (ImGui.Button(_isPaused ? "继续" : "暂停"))
        {
            _isPaused = !_isPaused;
        }

        ImGui.SameLine();

        // 清空当前会话日志
        if (ImGui.Button("清空日志"))
        {
            _currentSession.Packets.Clear();
            _selectedPacket = null;
            _selectedIndex = -1;
            _selectedIndices.Clear();
            _lastClickedIndex = -1;
        }

        ImGui.SameLine();

        // 新建会话
        if (ImGui.Button("新建会话"))
        {
            // 将当前会话设为非活跃
            _currentSession.IsActive = false;

            // 创建新会话
            _sessionCounter++;
            var newSession = new PacketSession(DateTime.Now.Ticks, $"会话 {_sessionCounter}");
            _sessions.Add(newSession);
            _currentSession = newSession;

            // 标记需要切换到新会话
            _pendingSessionSwitch = newSession.SessionId;

            _selectedPacket = null;
            _selectedIndex = -1;
            _selectedIndices.Clear();
            _lastClickedIndex = -1;
        }

        ImGui.SameLine();

        // 删除选中的包
        if (ImGui.Button("删除选中"))
        {
            DeleteSelectedPackets();
        }

        ImGui.SameLine();

        // 自动滚动 - 使用配置（使用局部变量）
        bool autoScroll = Plugin.Configuration.AutoScroll;
        if (ImGui.Checkbox("自动滚动", ref autoScroll))
        {
            Plugin.Configuration.AutoScroll = autoScroll;
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();

        // 启用捕获 - 使用配置（使用局部变量）
        bool captureEnabled = Plugin.Configuration.CaptureEnabled;
        if (ImGui.Checkbox("启用捕获", ref captureEnabled))
        {
            Plugin.Configuration.CaptureEnabled = captureEnabled;
            if (Plugin.MyNetRe != null)
            {
                Plugin.MyNetRe.CaptureEnabled = captureEnabled;
            }
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.Text($"| 总计: {_currentSession.Packets.Count} 包");
        if (_selectedIndices.Count > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"| 已选: {_selectedIndices.Count}");
        }

        // 过滤器
        ImGui.SetNextItemWidth(200);
        ImGui.InputTextWithHint("##filter", "搜索 Opcode...", ref _filterText, 128);

        ImGui.SameLine();

        // 发包过滤 - 使用配置（使用局部变量）
        bool showSendPackets = Plugin.Configuration.ShowSendPackets;
        if (ImGui.Checkbox("发包", ref showSendPackets))
        {
            Plugin.Configuration.ShowSendPackets = showSendPackets;
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();

        // 收包过滤 - 使用配置（使用局部变量）
        bool showReceivePackets = Plugin.Configuration.ShowReceivePackets;
        if (ImGui.Checkbox("收包", ref showReceivePackets))
        {
            Plugin.Configuration.ShowReceivePackets = showReceivePackets;
            Plugin.Configuration.Save();
        }

        ImGui.SameLine();

        // 仅已知Opcode - 使用配置（使用局部变量）
        bool showOnlyKnownOpcodes = Plugin.Configuration.ShowOnlyKnownOpcodes;
        if (ImGui.Checkbox("仅已知Opcode", ref showOnlyKnownOpcodes))
        {
            Plugin.Configuration.ShowOnlyKnownOpcodes = showOnlyKnownOpcodes;
            Plugin.Configuration.Save();
        }
    }

    private void DeleteSelectedPackets()
    {
        if (_selectedIndices.Count == 0)
            return;

        // 获取过滤后的包列表
        var filteredPackets = GetFilteredPackets();

        // 收集要删除的包
        var packetsToDelete = new List<PacketInfo>();
        foreach (var index in _selectedIndices.OrderBy(x => x))
        {
            if (index >= 0 && index < filteredPackets.Count)
            {
                packetsToDelete.Add(filteredPackets[index]);
            }
        }

        // 从当前会话中删除
        foreach (var packet in packetsToDelete)
        {
            _currentSession.Packets.Remove(packet);
        }

        // 清空选择
        _selectedPacket = null;
        _selectedIndex = -1;
        _selectedIndices.Clear();
        _lastClickedIndex = -1;
    }

    private void DrawSplitView()
    {
        var availableRegion = ImGui.GetContentRegionAvail();

        // 左侧包列表
        ImGui.BeginChild("PacketListPanel", new Vector2(_leftPanelWidth, availableRegion.Y), true);
        DrawPacketList();
        ImGui.EndChild();

        ImGui.SameLine();

        // 分隔条
        DrawSplitter(availableRegion.Y);

        ImGui.SameLine();

        // 右侧详细信息
        var rightPanelWidth = availableRegion.X - _leftPanelWidth - _splitterWidth;
        ImGui.BeginChild("PacketDetailPanel", new Vector2(rightPanelWidth, availableRegion.Y), true);
        DrawPacketDetail();
        ImGui.EndChild();
    }

    private void DrawSplitter(float height)
    {
        var splitterPos = ImGui.GetCursorScreenPos();
        var splitterSize = new Vector2(_splitterWidth, height);

        ImGui.InvisibleButton("Splitter", splitterSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
        }

        if (ImGui.IsItemActive())
        {
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _leftPanelWidth += ImGui.GetIO().MouseDelta.X;
                _leftPanelWidth = Math.Clamp(_leftPanelWidth, 300f, ImGui.GetContentRegionAvail().X - 300f);
            }
        }

        // 绘制分隔线
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(
            splitterPos,
            splitterPos + splitterSize,
            ImGui.GetColorU32(ImGui.IsItemHovered() ? ImGuiCol.ButtonHovered : ImGuiCol.Border)
        );
    }

    private void DrawPacketList()
    {
        ImGui.Text("包列表 (时间轴)");
        ImGui.Separator();

        // 表头
        if (ImGui.BeginTable("PacketTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("时间", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("方向", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn("Opcode", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("长度(以结构体大小为准)", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            // 过滤后的包列表
            var filteredPackets = GetFilteredPackets();

            for (int i = 0; i < filteredPackets.Count; i++)
            {
                var packet = filteredPackets[i];

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                // 可选择的行 - 支持多选
                bool isSelected = _selectedIndices.Contains(i);
                var rowColor = packet.Direction == PacketDirection.Send
                    ? new Vector4(0.2f, 0.3f, 0.5f, 0.3f)  // 发包：蓝色
                    : new Vector4(0.3f, 0.5f, 0.2f, 0.3f); // 收包：绿色

                // 设置行背景色
                if (isSelected)
                {
                    // 选中项：鲜红色背景
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.85f, 0.1f, 0.1f, 1.0f)));
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(new Vector4(0.85f, 0.1f, 0.1f, 1.0f)));

                    // 推送选中样式，使用更鲜艳的红色
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.9f, 0.1f, 0.1f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.95f, 0.15f, 0.15f, 1.0f));
                }
                else
                {
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(rowColor));
                }

                if (ImGui.Selectable($"##{i}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                {
                    var io = ImGui.GetIO();

                    if (io.KeyShift && _lastClickedIndex >= 0)
                    {
                        // Shift多选：选择范围
                        int start = Math.Min(_lastClickedIndex, i);
                        int end = Math.Max(_lastClickedIndex, i);

                        for (int j = start; j <= end; j++)
                        {
                            _selectedIndices.Add(j);
                        }
                    }
                    else if (io.KeyCtrl)
                    {
                        // Ctrl多选：切换选择状态
                        if (_selectedIndices.Contains(i))
                            _selectedIndices.Remove(i);
                        else
                            _selectedIndices.Add(i);
                    }
                    else
                    {
                        // 单选：清空其他选择
                        _selectedIndices.Clear();
                        _selectedIndices.Add(i);
                    }

                    _lastClickedIndex = i;
                    _selectedPacket = packet;
                    _selectedIndex = i;
                }

                // 右键菜单：删除单个包
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"PacketContextMenu{i}");
                }

                if (ImGui.BeginPopup($"PacketContextMenu{i}"))
                {
                    if (ImGui.MenuItem("重发此包"))
                    {
                        OpenResendWindow(packet);
                    }

                    if (ImGui.MenuItem("删除此包"))
                    {
                        _currentSession.Packets.Remove(packet);
                        _selectedIndices.Remove(i);
                        if (_selectedPacket == packet)
                        {
                            _selectedPacket = null;
                            _selectedIndex = -1;
                        }
                    }

                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                ImGui.Text(packet.TimeString);

                ImGui.TableNextColumn();
                ImGui.Text(packet.DirectionString);

                ImGui.TableNextColumn();
                ImGui.Text($"0x{packet.Opcode:X4}");

                ImGui.TableNextColumn();
                ImGui.Text(packet.OpcodeName);

                ImGui.TableNextColumn();
                ImGui.Text($"{packet.PacketLength}");

                // 如果是选中项，弹出之前推送的样式
                if (isSelected)
                {
                    ImGui.PopStyleColor(3);
                }
            }

            // 自动滚动到底部
            if (Plugin.Configuration.AutoScroll && filteredPackets.Count > 0)
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.EndTable();
        }
    }

    private List<PacketInfo> GetFilteredPackets()
    {
        return _currentSession.Packets.Where(p =>
        {
            // 方向过滤 - 使用配置
            if (p.Direction == PacketDirection.Send && !Plugin.Configuration.ShowSendPackets)
                return false;
            if (p.Direction == PacketDirection.Receive && !Plugin.Configuration.ShowReceivePackets)
                return false;

            // 仅显示已知Opcode - 使用配置
            if (Plugin.Configuration.ShowOnlyKnownOpcodes && p.OpcodeName == "Unknown")
                return false;

            // 文本过滤
            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                return p.OpcodeName.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                       p.Opcode.ToString("X4").Contains(_filterText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }).ToList();
    }

    private void DrawPacketDetail()
    {
        if (_selectedPacket == null)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "请从左侧列表选择一个包以查看详细信息");
            return;
        }

        var packet = _selectedPacket;

        ImGui.Text($"包详细信息 - {packet.OpcodeName}");
        ImGui.Separator();

        // 基本信息
        ImGui.Text($"时间: {packet.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
        ImGui.Text($"方向: {packet.DirectionString}");
        ImGui.Text($"Opcode: 0x{packet.Opcode:X4} ({packet.Opcode})");
        ImGui.Text($"名称: {packet.OpcodeName}");
        ImGui.Text($"长度: {packet.PacketLength} 字节");

        if (packet.Direction == PacketDirection.Send)
        {
            ImGui.Text($"优先级: {packet.Priority}");
        }
        else
        {
            ImGui.Text($"目标ID: 0x{packet.TargetID:X8}");
        }

        ImGui.Separator();

        // Tab 面板
        if (ImGui.BeginTabBar("PacketDetailTabs"))
        {
            // 十六进制视图
            if (ImGui.BeginTabItem("十六进制数据"))
            {
                DrawHexView(packet);
                ImGui.EndTabItem();
            }

            // 结构体解析视图
            if (ImGui.BeginTabItem("结构体解析"))
            {
                DrawStructView(packet);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawHexView(PacketInfo packet)
    {
        var hexDump = packet.GetHexDump();

        ImGui.BeginChild("HexDumpChild", new Vector2(0, -30), true, ImGuiWindowFlags.HorizontalScrollbar);
        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]); // 使用等宽字体
        ImGui.TextUnformatted(hexDump);
        ImGui.PopFont();
        ImGui.EndChild();

        // 复制按钮
        if (ImGui.Button("复制十六进制数据"))
        {
            ImGui.SetClipboardText(hexDump);
        }

        ImGui.SameLine();

        if (ImGui.Button("复制原始字节 (C# byte[])"))
        {
            var byteArrayStr = "byte[] data = new byte[] { " +
                string.Join(", ", packet.RawData.Select(b => $"0x{b:X2}")) + " };";
            ImGui.SetClipboardText(byteArrayStr);
        }
    }

    private void DrawStructView(PacketInfo packet)
    {
        // 动态查找结构体
        var structType = FindStructType(packet.OpcodeName);

        if (structType == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), $"未找到对应的结构体定义: {packet.OpcodeName}");
            ImGui.Text("请确保已经定义了对应的结构体，例如：");
            ImGui.TextWrapped($"[StructLayout(LayoutKind.Explicit, Size = 0x...)]\npublic unsafe struct {packet.OpcodeName}\n{{\n    [FieldOffset(0x00)] public ushort Field1;\n    // ...\n}}");
            return;
        }

        // 收包数据前有 0x20 (32) 字节的包头，发包前有 0x10 (16) 字节的包头，需要跳过
        const int RECEIVE_PACKET_HEADER_SIZE = 0x20;
        const int SEND_PACKET_HEADER_SIZE = 0x20;
        int headerOffset = packet.Direction == PacketDirection.Receive ? RECEIVE_PACKET_HEADER_SIZE : SEND_PACKET_HEADER_SIZE;

        // 显示结构体信息（带颜色）
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1f), $"✓ 找到结构体: {structType.Name}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), $"收包结构体不能保证正确性，如有差异请手动调整结构体。");
        ImGui.Text($"结构体大小: {Marshal.SizeOf(structType)} 字节");
        if (headerOffset > 0)
        {
            string directionStr = packet.Direction == PacketDirection.Receive ? "收包" : "发包";
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1f, 1f), $"{directionStr}包头偏移: +0x{headerOffset:X2} ({headerOffset}) 字节");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // 解析字段
        var fields = structType.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() != null)
            .OrderBy(f => f.GetCustomAttribute<FieldOffsetAttribute>()!.Value)
            .ToList();

        if (fields.Count == 0)
        {
            ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), "结构体中没有定义字段");
            return;
        }

        ImGui.Text($"字段总数: {fields.Count}");
        ImGui.Spacing();

        ImGui.BeginChild("StructFieldsChild", new Vector2(0, 0), true);

        // 推送表格样式，增加行间距和内边距
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 4f));

        if (ImGui.BeginTable("StructFields", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("偏移", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("字段名", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("类型", ImGuiTableColumnFlags.WidthFixed, 120f);
            ImGui.TableSetupColumn("值", ImGuiTableColumnFlags.WidthFixed, 180f);
            ImGui.TableHeadersRow();

            foreach (var field in fields)
            {
                var offsetAttr = field.GetCustomAttribute<FieldOffsetAttribute>()!;
                var offset = offsetAttr.Value;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"0x{offset:X2}");

                ImGui.TableNextColumn();
                ImGui.Text(field.Name);

                ImGui.TableNextColumn();
                ImGui.Text(field.FieldType.Name);

                ImGui.TableNextColumn();

                // 尝试读取值（收包需要加上包头偏移）
                try
                {
                    var k = ReadFieldValue(field.FieldType, packet.RawData, offset+headerOffset);
                    // 一般来说只有枚举需要原始数据
                    if (field.FieldType.IsEnum)
                    {
                        var name = Enum.GetName(field.FieldType, k) ?? k.ToString();
                        var underlying = Enum.GetUnderlyingType(field.FieldType);
                        var numeric = Convert.ChangeType(k, underlying);
                        ImGui.Text($"{name} ({numeric})");
                    }
                    else
                    {
                        ImGui.Text(k.ToString());
                    }
                    
                    // 这里去掉读取类型了
                    // var value = ReadFieldValue(packet.RawData, offset + headerOffset, field.FieldType, field.Name, structType, fields);
                    
                }
                catch(Exception e)
                {
                    ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), $"{e.Message}");
                }
            }

            ImGui.EndTable();
        }

        // 恢复表格样式
        ImGui.PopStyleVar(2);

        ImGui.EndChild();
    }

    
    public static object ReadFieldValue(Type type, byte[] data, int offset) {
        if (offset >= data.Length) return "超出范围";
        var method = typeof(StructConverter)
                     .GetMethod(nameof(StructConverter.FromBytesGeneric), BindingFlags.Static | BindingFlags.Public)
                     ?.MakeGenericMethod(type);

        return method.Invoke(null, [data, offset]);
    }

    public static class StructConverter {
        public static T FromBytesGeneric<T>(byte[] data, int offset) where T : struct {
            return MemoryMarshal.Cast<byte, T>(data.AsSpan(offset))[0];
        }
    }
    
    
    private Type? FindStructType(string opcodeName)
    {
        // 检查缓存
        if (_structTypeCache.TryGetValue(opcodeName, out var cachedType))
            return cachedType;

        try
        {
            // 在当前程序集中查找
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            Plugin.Log.Debug($"[结构体查找] 开始查找: {opcodeName}, 程序集中共有 {types.Length} 个类型");

            foreach (var type in types)
            {
                // 详细记录匹配过程
                if (type.Name == opcodeName)
                {
                    Plugin.Log.Debug($"[结构体查找] 找到名称匹配: {type.FullName}");
                    Plugin.Log.Debug($"[结构体查找]   IsValueType: {type.IsValueType}");

                    // 尝试多种方式获取 StructLayout 特性
                    var structLayoutAttr = type.GetCustomAttribute<StructLayoutAttribute>(inherit: false);

                    // 如果第一种方式失败，尝试直接从 CustomAttributes 查找
                    if (structLayoutAttr == null)
                    {
                        var attrs = type.GetCustomAttributesData();
                        foreach (var attr in attrs)
                        {
                            if (attr.AttributeType == typeof(StructLayoutAttribute))
                            {
                                structLayoutAttr = (StructLayoutAttribute)Attribute.GetCustomAttribute(type, typeof(StructLayoutAttribute));
                                break;
                            }
                        }
                    }

                    Plugin.Log.Debug($"[结构体查找]   StructLayout: {structLayoutAttr != null}");

                    // 对于 unsafe struct，只要是 ValueType 就认为是有效的
                    if (type.IsValueType)
                    {
                        _structTypeCache[opcodeName] = type;
                        Plugin.Log.Info($"[结构体查找] ✓ 成功找到结构体: {type.FullName} (Size: {Marshal.SizeOf(type)} 字节)");
                        return type;
                    }
                }
            }

            Plugin.Log.Warning($"[结构体查找] ✗ 未找到结构体: {opcodeName}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"[结构体查找] 查找时出错: {ex.Message}");
        }

        _structTypeCache[opcodeName] = null;
        return null;
    }

    private unsafe string ReadFieldValue(byte[] data, int offset, Type fieldType, string fieldName, Type? structType = null, List<FieldInfo>? allFields = null)
    {
        if (offset >= data.Length)
            return "超出范围";

        fixed (byte* ptr = data)
        {
            if (fieldType == typeof(byte))
                return offset < data.Length ? $"0x{data[offset]:X2}" : "N/A";

            if (fieldType == typeof(ushort))
            {
                if (offset + 1 < data.Length)
                {
                    ushort value = *(ushort*)(ptr + offset);

                    // 特殊处理：如果是 ActorControl 相关结构体的 Id 字段，显示枚举名称
                    if (fieldName == "Id" && structType != null &&
                        (structType.Name == "DOWN_ActorControl" ||
                         structType.Name == "DOWN_ActorControlSelf" ||
                         structType.Name == "DOWN_ActorControlTarget"))
                    {
                        if (System.Enum.IsDefined(typeof(ActorControlId), value))
                        {
                            var enumName = ((ActorControlId)value).ToString();
                            return $"0x{value:X4} ({enumName})";
                        }
                        else
                        {
                            return $"0x{value:X4} (Unknown)";
                        }
                    }

                    return $"0x{value:X4}";
                }
                return "N/A";
            }

            if (fieldType == typeof(uint))
                return offset + 3 < data.Length ? $"0x{*(uint*)(ptr + offset):X8}" : "N/A";

            if (fieldType == typeof(ulong))
                return offset + 7 < data.Length ? $"0x{*(ulong*)(ptr + offset):X16}" : "N/A";

            if (fieldType == typeof(short))
                return offset + 1 < data.Length ? $"{*(short*)(ptr + offset)}" : "N/A";

            if (fieldType == typeof(int))
                return offset + 3 < data.Length ? $"{*(int*)(ptr + offset)}" : "N/A";

            if (fieldType == typeof(long))
                return offset + 7 < data.Length ? $"{*(long*)(ptr + offset)}" : "N/A";

            if (fieldType == typeof(float))
                return offset + 3 < data.Length ? $"{*(float*)(ptr + offset):F2}" : "N/A";

            if (fieldType == typeof(double))
                return offset + 7 < data.Length ? $"{*(double*)(ptr + offset):F2}" : "N/A";

            // 处理 fixed 数组（编译器会生成特殊的嵌套类型，类型名包含 "<" 和 ">"）
            if (fieldType.Name.Contains("<") || fieldType.IsNestedPublic)
            {
                // fixed byte[] 数组
                if (fieldType.Name.Contains("Byte") || fieldName.ToLower().Contains("name") || fieldName.ToLower().Contains("bytes"))
                {
                    int remainingBytes = Math.Min(32, data.Length - offset);
                    if (remainingBytes > 0)
                    {
                        var bytes = new byte[remainingBytes];
                        Array.Copy(data, offset, bytes, 0, remainingBytes);
                        return BitConverter.ToString(bytes).Replace("-", " ");
                    }
                    return "N/A";
                }

                // fixed uint[] 数组（例如 args 字段）
                // 尝试读取 arg_cnt 字段来确定实际元素数量
                int arrayCount = 4; // 默认显示前 4 个

                if (structType != null && allFields != null)
                {
                    var argCntField = allFields.FirstOrDefault(f => f.Name == "arg_cnt");
                    if (argCntField != null)
                    {
                        var argCntOffset = argCntField.GetCustomAttribute<FieldOffsetAttribute>()?.Value ?? 0;

                        // 计算实际数据位置：需要加上包头偏移
                        // offset 参数已经包含了包头偏移，所以我们需要计算相对位置
                        // 当前字段的结构体偏移可以通过反推得到
                        int currentFieldOffset = 0x8; // args 字段固定在 0x8
                        int headerOffset = offset - currentFieldOffset; // 反推包头偏移
                        int actualArgCntOffset = headerOffset + argCntOffset;

                        if (actualArgCntOffset < data.Length)
                        {
                            arrayCount = data[actualArgCntOffset];
                            if (arrayCount == 0)
                            {
                                return "[0] (空数组)";
                            }
                            arrayCount = Math.Min(arrayCount, 10); // 最多显示 10 个
                        }
                    }
                }

                if (offset + arrayCount * 4 <= data.Length)
                {
                    var values = new List<string>();
                    for (int i = 0; i < arrayCount; i++)
                    {
                        uint value = *(uint*)(ptr + offset + i * 4);
                        values.Add($"0x{value:X8}");
                    }
                    return $"[{arrayCount}] {string.Join(", ", values)}";
                }
                return $"[数组] 数据不足";
            }

            return $"未知类型 ({fieldType.Name})";
        }
    }

    private void OpenResendWindow(PacketInfo packet)
    {
        // 查找对应的结构体
        var structType = FindStructType(packet.OpcodeName);

        // 生成唯一的窗口ID
        var windowId = $"ResendPacket{packet.Timestamp.Ticks}";

        // 检查是否已经存在相同的窗口
        var existingWindow = Plugin.WindowSystem.Windows.FirstOrDefault(w => w.WindowName.Contains(windowId));
        if (existingWindow != null)
        {
            // 如果窗口已存在，直接打开它
            existingWindow.IsOpen = true;
            return;
        }

        // 创建重发窗口
        var resendWindow = new PacketResendWindow(Plugin, packet, structType);

        // 添加到窗口系统
        Plugin.WindowSystem.AddWindow(resendWindow);
    }
}
