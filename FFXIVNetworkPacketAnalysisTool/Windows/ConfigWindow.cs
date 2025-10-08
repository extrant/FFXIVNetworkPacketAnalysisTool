using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using FFXIVNetworkPacketAnalysisTool.Utils;

namespace FFXIVNetworkPacketAnalysisTool.Windows;

public unsafe class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin Plugin;

    // 动画时间
    private float _animationTime = 0f;
    private int _selectedTab = 0;

    // 更新日志数据
    private readonly List<ChangelogEntry> _changelog = new List<ChangelogEntry>
    {
        new ChangelogEntry
        {
            Version = "v1.2.0",
            Date = "2025-10-08",
            Type = ChangeType.Feature,
            Changes = new List<string>
            {
                "新增多会话支持，可同时管理多个抓包会话",
                "新增包过滤功能，支持发包/收包/已知Opcode过滤",
                "新增结构体自动解析，自动识别已定义的包结构",
                "新增多选功能，支持 Ctrl/Shift 批量选择和删除",
                "优化UI界面，添加颜色区分和鼠标悬停效果"
            }
        },
        new ChangelogEntry
        {
            Version = "v1.1.5",
            Date = "2025-10-07",
            Type = ChangeType.Fix,
            Changes = new List<string>
            {
                "修复长时间运行导致的内存泄漏问题",
                "修复结构体解析时的偏移量计算错误",
                "优化包捕获性能，减少CPU占用",
                "修复包列表滚动时的卡顿问题"
            }
        },
        new ChangelogEntry
        {
            Version = "v1.1.0",
            Date = "2025-10-06",
            Type = ChangeType.Feature,
            Changes = new List<string>
            {
                "新增包体结构体解析功能",
                "新增十六进制视图和C#数组导出",
                "新增自动滚动开关",
                "新增配置持久化保存"
            }
        },
        new ChangelogEntry
        {
            Version = "v1.0.0",
            Date = "2025-10-06",
            Type = ChangeType.Initial,
            Changes = new List<string>
            {
                "首个正式版本发布",
                "实现基础网络包捕获功能",
                "实现包列表展示和详情查看",
                "支持Opcode名称映射"
            }
        }
    };

    public ConfigWindow(Plugin plugin) : base("About FFXIV NPATool###AboutWindow")
    {
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(750, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        Configuration = plugin.Configuration;
        Plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        // 更新动画时间
        _animationTime += ImGui.GetIO().DeltaTime;
    }

    public override void Draw()
    {
        DrawHeader();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawTabBar();
    }

    private void DrawHeader()
    {
        var windowWidth = ImGui.GetWindowWidth();

        // 绘制彩色渐变背景
        var drawList = ImGui.GetWindowDrawList();
        var headerMin = ImGui.GetCursorScreenPos();
        var headerMax = new Vector2(headerMin.X + windowWidth - 15, headerMin.Y + 120);

        // 多层渐变效果
        uint color1 = ImGui.GetColorU32(new Vector4(0.2f, 0.4f, 0.8f, 0.3f));
        uint color2 = ImGui.GetColorU32(new Vector4(0.6f, 0.3f, 0.9f, 0.3f));
        uint color3 = ImGui.GetColorU32(new Vector4(0.3f, 0.7f, 0.9f, 0.3f));

        drawList.AddRectFilledMultiColor(headerMin, headerMax, color1, color2, color3, color1);

        ImGui.Dummy(new Vector2(0, 10));

        // 工具标题 - 居中 + 动画
        var titleText = "FFXIV Network Packet Analysis Tool";
        var titleSize = ImGui.CalcTextSize(titleText);
        ImGui.SetCursorPosX((windowWidth - titleSize.X) * 0.5f);

        // 彩虹色动画
        float hue = (_animationTime * 0.3f) % 1.0f;
        var titleColor = ColorFromHSV(hue, 0.8f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]);
        ImGui.Text(titleText);
        ImGui.PopFont();
        ImGui.PopStyleColor();

        ImGui.Spacing();

        // 副标题
        var subtitleText = "FF14 网络包分析工具";
        var subtitleSize = ImGui.CalcTextSize(subtitleText);
        ImGui.SetCursorPosX((windowWidth - subtitleSize.X) * 0.5f);
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), subtitleText);

        ImGui.Spacing();

        // 版本和作者信息 - 居中
        var versionText = "Version 1.2.0 | By Siren";
        var versionSize = ImGui.CalcTextSize(versionText);
        ImGui.SetCursorPosX((windowWidth - versionSize.X) * 0.5f);
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1f), versionText);

        ImGui.Spacing();

        // 闪烁的心形符号
        float pulse = (float)Math.Sin(_animationTime * 3.0f) * 0.3f + 0.7f;
        var heartText = "Made with ♥ for Dalamud Plugin Developer";
        var heartSize = ImGui.CalcTextSize(heartText);
        ImGui.SetCursorPosX((windowWidth - heartSize.X) * 0.5f);
        ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, pulse), heartText);

        ImGui.Dummy(new Vector2(0, 10));
    }

    private void DrawTabBar()
    {
        if (ImGui.BeginTabBar("AboutTabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("更新日志"))
            {
                _selectedTab = 0;
                DrawChangelogTab();
                ImGui.EndTabItem();
            }


            ImGui.EndTabBar();
        }
    }

    private void DrawChangelogTab()
    {
        ImGui.BeginChild("ChangelogScroll", new Vector2(0, 450), true);

        foreach (var entry in _changelog)
        {
            // 版本标题块
            DrawChangelogHeader(entry);

            ImGui.Indent(20);

            // 变更列表
            foreach (var change in entry.Changes)
            {
                ImGui.TextWrapped(change);
                ImGui.Spacing();
            }

            ImGui.Unindent(20);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        ImGui.EndChild();
    }

    private void DrawChangelogHeader(ChangelogEntry entry)
    {
        Vector4 typeColor = entry.Type switch
        {
            ChangeType.Feature => new Vector4(0.4f, 0.8f, 0.4f, 1f),
            ChangeType.Fix => new Vector4(1.0f, 0.6f, 0.2f, 1f),
            ChangeType.Breaking => new Vector4(1.0f, 0.3f, 0.3f, 1f),
            ChangeType.Initial => new Vector4(0.6f, 0.4f, 1.0f, 1f),
            _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
        };

        string typeIcon = entry.Type switch
        {
            ChangeType.Feature => "新功能",
            ChangeType.Fix => "修复",
            ChangeType.Breaking => "破坏性变更",
            ChangeType.Initial => "初始版本",
            _ => "更新"
        };

        ImGui.PushStyleColor(ImGuiCol.Text, typeColor);
        ImGui.Text($"{entry.Version} - {typeIcon}");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"({entry.Date})");
    }

    private void DrawAuthorTab()
    {
        ImGui.BeginChild("AuthorScroll", new Vector2(0, 450), true);

        var windowWidth = ImGui.GetContentRegionAvail().X;

        ImGui.Dummy(new Vector2(0, 20));

        // 作者头像框（使用ASCII艺术）
        var avatarLines = new[]
        {
            "    ╔════════════════════╗",
            "    ║                    ║",
            "    ║    👨‍💻  SirenPVP   ║",
            "    ║                    ║",
            "    ╚════════════════════╝"
        };

        foreach (var line in avatarLines)
        {
            var lineSize = ImGui.CalcTextSize(line);
            ImGui.SetCursorPosX((windowWidth - lineSize.X) * 0.5f);
            ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1f), line);
        }

        ImGui.Spacing();
        ImGui.Spacing();

        // 开发者信息
        CenteredText("🎮 最终幻想14 玩家", new Vector4(0.8f, 0.8f, 0.8f, 1f));
        CenteredText("💻 网络协议分析爱好者", new Vector4(0.8f, 0.8f, 0.8f, 1f));
        CenteredText("🛠️ 工具开发者", new Vector4(0.8f, 0.8f, 0.8f, 1f));

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();

        // 联系方式
        CenteredText("📫 联系方式", new Vector4(0.4f, 0.8f, 1.0f, 1f));
        ImGui.Spacing();

        DrawLinkButton("🔗 GitHub", "https://github.com/SirenPVP", windowWidth);
        DrawLinkButton("💬 Discord", "SirenPVP#0000", windowWidth);
        DrawLinkButton("📧 Email", "contact@sirenpvp.com", windowWidth);

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();

        // 特别感谢
        CenteredText("🙏 特别感谢", new Vector4(1.0f, 0.8f, 0.4f, 1f));
        ImGui.Spacing();

        ImGui.TextWrapped("• FFXIV 社区的所有贡献者");
        ImGui.TextWrapped("• Dalamud 框架开发团队");
        ImGui.TextWrapped("• 所有测试和反馈的玩家");
        ImGui.TextWrapped("• ImGui 图形界面库");

        ImGui.Spacing();
        ImGui.Spacing();

        // 闪烁的感谢语
        float alpha = (float)Math.Sin(_animationTime * 2.0f) * 0.3f + 0.7f;
        CenteredText("感谢您使用此工具！", new Vector4(1.0f, 0.7f, 0.7f, alpha));

        ImGui.Dummy(new Vector2(0, 20));

        ImGui.EndChild();
    }

    private void DrawFeaturesTab()
    {
        ImGui.BeginChild("FeaturesScroll", new Vector2(0, 450), true);

        DrawFeatureSection("📡 网络包捕获", new[]
        {
            "实时捕获游戏网络封包",
            "自动识别 Opcode 名称",
            "支持发包和收包双向捕获",
            "高性能异步处理，不影响游戏性能"
        });

        DrawFeatureSection("📊 数据分析", new[]
        {
            "十六进制数据查看",
            "C# 结构体自动解析",
            "字段值实时显示",
            "支持导出为 C# byte[] 数组"
        });

        DrawFeatureSection("🎯 会话管理", new[]
        {
            "多会话并行支持",
            "会话独立管理",
            "快速切换和关闭",
            "自动限制缓存大小"
        });

        DrawFeatureSection("🔍 过滤搜索", new[]
        {
            "Opcode 名称搜索",
            "发包/收包类型过滤",
            "仅显示已知 Opcode",
            "多条件组合过滤"
        });

        DrawFeatureSection("✨ 界面功能", new[]
        {
            "Ctrl/Shift 多选支持",
            "拖拽调整面板大小",
            "自动滚动到最新包",
            "颜色区分包类型",
            "右键快捷菜单"
        });

        ImGui.EndChild();
    }

    private void DrawSettingsTab()
    {
        ImGui.BeginChild("SettingsScroll", new Vector2(0, 450), true);

        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1f), "⚙️ 常规设置");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // 最大包数量
        int maxPackets = Configuration.MaxPacketsPerSession;
        if (ImGui.SliderInt("每会话最大包数量", ref maxPackets, 100, 10000))
        {
            Configuration.MaxPacketsPerSession = maxPackets;
            Configuration.Save();
        }
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "超出此数量将自动删除最旧的包");

        ImGui.Spacing();
        ImGui.Spacing();

        // 自动滚动
        bool autoScroll = Configuration.AutoScroll;
        if (ImGui.Checkbox("启用自动滚动", ref autoScroll))
        {
            Configuration.AutoScroll = autoScroll;
            Configuration.Save();
        }
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "自动滚动到最新捕获的包");

        ImGui.Spacing();
        ImGui.Spacing();

        // 默认显示过滤
        bool showSend = Configuration.ShowSendPackets;
        if (ImGui.Checkbox("默认显示发包", ref showSend))
        {
            Configuration.ShowSendPackets = showSend;
            Configuration.Save();
        }

        bool showReceive = Configuration.ShowReceivePackets;
        if (ImGui.Checkbox("默认显示收包", ref showReceive))
        {
            Configuration.ShowReceivePackets = showReceive;
            Configuration.Save();
        }

        bool showOnlyKnown = Configuration.ShowOnlyKnownOpcodes;
        if (ImGui.Checkbox("默认仅显示已知 Opcode", ref showOnlyKnown))
        {
            Configuration.ShowOnlyKnownOpcodes = showOnlyKnown;
            Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.4f, 1f), "⚠️ 高级设置");
        ImGui.Spacing();

        // 重置按钮
        if (ImGui.Button("🔄 重置所有设置", new Vector2(200, 30)))
        {
            ImGui.OpenPopup("ResetConfirm");
        }

        // 确认弹窗
        bool resetPopupOpen = true;
        if (ImGui.BeginPopupModal("ResetConfirm", ref resetPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("确定要重置所有设置吗？");
            ImGui.Spacing();

            if (ImGui.Button("确定", new Vector2(120, 0)))
            {
                Configuration.MaxPacketsPerSession = 5000;
                Configuration.AutoScroll = true;
                Configuration.ShowSendPackets = true;
                Configuration.ShowReceivePackets = true;
                Configuration.ShowOnlyKnownOpcodes = false;
                Configuration.Save();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("取消", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "配置文件位置:");
        ImGui.TextWrapped(Plugin.PluginInterface.ConfigFile.FullName);

        ImGui.EndChild();
    }

    private void DrawFeatureSection(string title, string[] features)
    {
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1f), title);
        ImGui.Spacing();

        ImGui.Indent(15);
        foreach (var feature in features)
        {
            ImGui.BulletText(feature);
        }
        ImGui.Unindent(15);

        ImGui.Spacing();
        ImGui.Spacing();
    }

    private void CenteredText(string text, Vector4 color)
    {
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(text);
        ImGui.SetCursorPosX((windowWidth - textSize.X) * 0.5f);
        ImGui.TextColored(color, text);
    }

    private void DrawLinkButton(string label, string value, float windowWidth)
    {
        var buttonWidth = 300f;
        ImGui.SetCursorPosX((windowWidth - buttonWidth) * 0.5f);

        if (ImGui.Button($"{label}: {value}", new Vector2(buttonWidth, 25)))
        {
            ImGui.SetClipboardText(value);
            Plugin.Log.Info($"已复制到剪贴板: {value}");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("点击复制到剪贴板");
        }
    }

    private Vector4 ColorFromHSV(float h, float s, float v)
    {
        float r = 0, g = 0, b = 0;

        if (s == 0.0f)
        {
            r = g = b = v;
        }
        else
        {
            h = h * 6.0f;
            int i = (int)h;
            float f = h - i;
            float p = v * (1.0f - s);
            float q = v * (1.0f - s * f);
            float t = v * (1.0f - s * (1.0f - f));

            switch (i)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
        }

        return new Vector4(r, g, b, 1.0f);
    }
}

// 更新日志条目
public class ChangelogEntry
{
    public string Version { get; set; } = "";
    public string Date { get; set; } = "";
    public ChangeType Type { get; set; }
    public List<string> Changes { get; set; } = new List<string>();
}

public enum ChangeType
{
    Feature,    // 新功能
    Fix,        // 修复
    Breaking,   // 破坏性变更
    Initial,    // 初始版本
    Other       // 其他
}
