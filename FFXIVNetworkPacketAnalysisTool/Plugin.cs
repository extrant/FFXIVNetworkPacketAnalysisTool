using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVNetworkPacketAnalysisTool.Utils;
using FFXIVNetworkPacketAnalysisTool.Windows;
using System;
using System.ComponentModel;
using System.IO;

namespace FFXIVNetworkPacketAnalysisTool;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    private const string CommandName = "/FFNPAT";



    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FFXIVNetworkPacketAnalysisTool");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private OnlineOpcode onlineOpcode;
    public static Lumina.GameData LuminaGameData => DataManager.GameData;
    public NetRe MyNetRe { get; private set; } = null!;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "background.png");


        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);
        //ESP = new ESP(this, MainWindow);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "打开主窗口"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        onlineOpcode = new OnlineOpcode(this);

        onlineOpcode.Run();

        MyNetRe = new NetRe(Configuration);

    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        MyNetRe?.Dispose();
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();

}
